using System.Collections.Concurrent;
using System.Text;
using Discord;
using Discord.WebSocket;
using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Models.Config;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Runs the job pipeline: accepts requests, hands them to workers in
    /// priority order, enforces deadlines, and returns results to Discord.
    /// </summary>
    /// <remarks>
    /// Membership of the worker pool lives in <see cref="WorkerRegistry"/> and
    /// queueing in <see cref="JobQueue"/>; what is left here is the loop that
    /// ties them together and the Discord side of delivering a result.
    /// </remarks>
    public class WorkerManagerService
    {
        private readonly ILogger<WorkerManagerService> logger;
        private readonly DiscordSocketClient discord;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly JobMetricsService metrics;
        private readonly WorkerSettings settings;
        private readonly Timer jobDispatchTimer;

        /// <summary>
        /// Jobs handed to a worker and not yet accounted for.
        /// </summary>
        /// <remarks>
        /// Core's copy is the authoritative one: a worker posts the whole job
        /// back, and only its <see cref="JobResult"/> is taken from that. It
        /// also decides whether a result is still wanted — a job that already
        /// timed out is gone from here, so its late result is dropped instead of
        /// arriving in the channel minutes after the apology did.
        /// </remarks>
        private readonly ConcurrentDictionary<Guid, WorkerJob> inFlight = new();

        public WorkerRegistry Registry { get; }

        public JobQueue Queue { get; }

        /// <summary>
        /// "Your request was accepted" messages to delete once the real result
        /// lands, keyed by job.
        /// </summary>
        public ConcurrentDictionary<Guid, ulong> AcceptanceNotifications { get; } = new();

        /// <summary>Workers as they currently stand. Backs the admin page.</summary>
        public IReadOnlyList<Worker> Workers => Registry.Snapshot();

        public WorkerManagerService(
            ILogger<WorkerManagerService> logger,
            DiscordSocketClient discord,
            IHttpClientFactory httpClientFactory,
            WorkerRegistry registry,
            JobQueue queue,
            JobMetricsService metrics,
            WorkerSettings settings)
        {
            this.logger = logger;
            this.discord = discord;
            this.httpClientFactory = httpClientFactory;
            this.metrics = metrics;
            this.settings = settings;
            Registry = registry;
            Queue = queue;

            jobDispatchTimer = new Timer(1000);
            jobDispatchTimer.Elapsed += JobDispatchTimer_Elapsed;
            jobDispatchTimer.AutoReset = false;
            jobDispatchTimer.Enabled = true;
        }

        /// <summary>
        /// Adds a new job for workers to work on.
        /// </summary>
        /// <param name="job">Job to work on</param>
        /// <returns>Status message, and the job id if it was accepted</returns>
        public Tuple<string, Guid?> EnqueueJob(WorkerJob job)
        {
            var now = DateTimeOffset.UtcNow;
            var profile = CommandRegistry.For(job.Command);

            job.Priority = profile.Priority;
            job.EnqueuedAt = now;
            job.TimeoutSeconds = (int)settings.TimeoutFor(job.Command).TotalSeconds;

            logger.LogInformation("Incoming worker request {JobId} for {Command} ({Capability}, {Priority} priority).",
                job.Id, job.Command, profile.Capability, job.Priority);

            if (!Registry.AnyCouldServe(profile.Capability, now))
            {
                logger.LogWarning("Job {JobId} - no workers are available that can satisfy this request.", job.Id);
                metrics.Record(job, JobOutcome.Dropped, null, now);
                return new Tuple<string, Guid?>(
                    "No workers are available that can satisfy this request. Try again later.", null);
            }

            Queue.Enqueue(job);
            return new Tuple<string, Guid?>("Your request was accepted.", job.Id);
        }

        /// <summary>
        /// Handles a returned result from a worker.
        /// </summary>
        /// <param name="job">Completed job, as the worker posted it back</param>
        public async Task ReturnResultAsync(WorkerJob job)
        {
            if (!inFlight.TryRemove(job.Id, out var tracked))
            {
                // Either this job already timed out and we apologised for it, or
                // a worker posted the same result twice. Both are quiet drops.
                logger.LogWarning("Discarding result for job {JobId}, which is no longer in flight.", job.Id);
                return;
            }

            // Take only the result from the worker's copy. Everything else —
            // which channel to answer in, when the job started — is ours.
            tracked.Result = job.Result;

            var worker = Registry.FindByJob(tracked.Id);
            await CompleteJob(tracked, JobOutcome.Completed, worker, workerAtFault: false);
        }

        /// <summary>
        /// Holds a worker, taking it out of rotation until it is readied by hand.
        /// </summary>
        public bool HoldWorker(Guid workerId) => Registry.Hold(workerId);

        /// <summary>
        /// Returns a held worker to rotation, clearing any tripped breaker.
        /// </summary>
        public bool ReadyWorker(Guid workerId) => Registry.Ready(workerId);

        private async void JobDispatchTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                await EvictStaleWorkers(now);
                await FailOverdueJobs(now);
                await DispatchPending(now);
            }
            catch (Exception ex)
            {
                // The dispatch loop stopping is the one failure that takes the
                // whole pipeline with it, so nothing is allowed out of here.
                logger.LogError(ex, "Job dispatch pass failed.");
            }
            finally
            {
                jobDispatchTimer.Enabled = true;
            }
        }

        /// <summary>
        /// Drops workers that stopped checking in, and fails whatever they were
        /// holding — nobody is going to post those results now.
        /// </summary>
        private async Task EvictStaleWorkers(DateTimeOffset now)
        {
            foreach (var evicted in Registry.SweepStale(now))
            {
                if (evicted.CurrentJob == Guid.Empty || !inFlight.TryRemove(evicted.CurrentJob, out var job))
                {
                    continue;
                }

                logger.LogWarning("Job {JobId} was running on {Worker}, which has gone away.", job.Id, evicted.Name);
                job.Result = new JobResult
                {
                    Message = "The worker handling your request went offline. Please try again."
                };

                await CompleteJob(job, JobOutcome.DispatchFailed, evicted, workerAtFault: true);
            }
        }

        /// <summary>
        /// Abandons jobs that have run past their deadline.
        /// </summary>
        /// <remarks>
        /// This is the fix for a wedged job holding a worker hostage. The worker
        /// is told to drop the job so it can take new work, and is charged a
        /// failure — a worker that keeps timing out is one to stop using.
        /// </remarks>
        private async Task FailOverdueJobs(DateTimeOffset now)
        {
            foreach (var worker in Registry.FindOverdue(now))
            {
                var jobId = worker.CurrentJob;
                if (!inFlight.TryRemove(jobId, out var job))
                {
                    continue;
                }

                var ranFor = now - (job.DispatchedAt ?? now);
                logger.LogWarning("Job {JobId} on {Worker} passed its {Timeout}s deadline after {Ran}; abandoning it.",
                    jobId, worker.Name, job.TimeoutSeconds, ranFor);

                job.Result = new JobResult
                {
                    Message = $"Your request hit its {job.TimeoutSeconds}s time limit and was given up on."
                };

                await AbandonOnWorker(worker, jobId);
                await CompleteJob(job, JobOutcome.TimedOut, worker, workerAtFault: true);
            }
        }

        /// <summary>
        /// Walks the queue high lane first, handing each job to a worker that
        /// can take it.
        /// </summary>
        private async Task DispatchPending(DateTimeOffset now)
        {
            foreach (var job in Queue.PendingInOrder())
            {
                var capability = CommandRegistry.For(job.Command).Capability;
                var worker = Registry.TryClaim(capability, job, settings.TimeoutFor(job.Command), now);

                if (worker == null)
                {
                    // Nothing free for this capability right now. If nothing
                    // could ever take it, give up rather than queueing forever.
                    if (!Registry.AnyCouldServe(capability, now))
                    {
                        Queue.Remove(job);
                        logger.LogWarning("Job {JobId} is being dropped; every worker that could run it is out of service.",
                            job.Id);
                        job.Result = new JobResult
                        {
                            Message = "The required worker went out of service before your request could be processed. Please try again later."
                        };
                        await CompleteJob(job, JobOutcome.Dropped, null, workerAtFault: false);
                    }

                    continue;
                }

                Queue.Remove(job);
                job.DispatchedAt = now;
                inFlight[job.Id] = job;

                if (!await SendToWorker(worker, job))
                {
                    inFlight.TryRemove(job.Id, out _);
                    job.Result = new JobResult
                    {
                        Message = "An error occurred while sending your request to a worker."
                    };
                    await CompleteJob(job, JobOutcome.DispatchFailed, worker, workerAtFault: true);
                }
            }
        }

        /// <summary>
        /// Hands a job to a worker.
        /// </summary>
        /// <returns>False if the worker refused it or could not be reached.</returns>
        private async Task<bool> SendToWorker(Worker worker, WorkerJob job)
        {
            logger.LogInformation("Job {JobId} is being assigned to {Worker}.", job.Id, worker.Name);

            try
            {
                var http = httpClientFactory.CreateClient();
                http.BaseAddress = new Uri(worker.Endpoint);
                http.Timeout = TimeSpan.FromSeconds(30);

                var payload = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");
                var response = await http.PostAsync("/api/worker", payload);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError("Worker {Worker} failed to accept job {JobId}. {Message}",
                    worker.Name, job.Id, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Tells a worker to stop caring about a job we have given up on, so it
        /// can accept new work instead of staying busy on a result nobody wants.
        /// Best effort: a worker too wedged to answer is exactly the case where
        /// this fails, and its own deadline covers that.
        /// </summary>
        private async Task AbandonOnWorker(Worker worker, Guid jobId)
        {
            try
            {
                var http = httpClientFactory.CreateClient();
                http.BaseAddress = new Uri(worker.Endpoint);
                http.Timeout = TimeSpan.FromSeconds(10);
                await http.DeleteAsync($"/api/worker/job/{jobId}");
            }
            catch (Exception ex)
            {
                logger.LogWarning("Could not tell {Worker} to abandon job {JobId}. {Message}",
                    worker.Name, jobId, ex.Message);
            }
        }

        /// <summary>
        /// The single exit for every job: release the worker, record what
        /// happened, and answer the user.
        /// </summary>
        private async Task CompleteJob(WorkerJob job, JobOutcome outcome, Worker? worker, bool workerAtFault)
        {
            var now = DateTimeOffset.UtcNow;

            if (worker != null)
            {
                Registry.Release(worker.Id, job.Id, workerAtFault, now);
            }

            metrics.Record(job, outcome, worker, now);
            await DeliverToDiscord(job);
        }

        private async Task DeliverToDiscord(WorkerJob job)
        {
            if (job.Result == null)
            {
                logger.LogWarning("Job {JobId} finished with no result to deliver.", job.Id);
                return;
            }

            logger.LogInformation("Job {JobId} is being returned.", job.Id);

            try
            {
                IGuild guild = FindServer(job.GuildId);
                ITextChannel channel = FindTextChannel(guild, job.ChannelId);

                if (channel == null)
                {
                    logger.LogWarning("Job {JobId} has nowhere to be delivered; guild or channel is gone.", job.Id);
                    return;
                }

                if (job.Result.FileName != null && job.Result.Data != null)
                {
                    logger.LogInformation("Job file name is {FileName}, {Bytes} bytes.",
                        job.Result.FileName, job.Result.Data.Length);
                    await channel.SendFileAsync(new MemoryStream(job.Result.Data), job.Result.FileName,
                        job.Result.Message, messageReference: new MessageReference(job.MessageId));
                }
                else
                {
                    logger.LogWarning("FileName or Data was null. Message {Message}", job.Result.Message);
                    await channel.SendMessageAsync(job.Result.Message,
                        messageReference: new MessageReference(job.MessageId));
                }

                await CleanUpAcceptanceNotification(job, channel);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to send result for job {JobId}. {Message}", job.Id, ex.Message);
            }
        }

        private async Task CleanUpAcceptanceNotification(WorkerJob job, ITextChannel channel)
        {
            // Removed whether or not the delete succeeds: leaving the entry
            // behind is how this dictionary used to grow without bound.
            if (!AcceptanceNotifications.TryRemove(job.Id, out var notificationId))
            {
                return;
            }

            try
            {
                await channel.DeleteMessagesAsync(new[] { notificationId });
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to clean up acceptance message for job {JobId}. {Message}",
                    job.Id, ex.Message);
            }
        }

        private IGuild FindServer(ulong id)
        {
            foreach (IGuild server in discord.Guilds)
            {
                if (server.Id == id)
                    return server;
            }
            return null;
        }

        private ITextChannel FindTextChannel(IGuild server, ulong id)
        {
            if (server == null)
            {
                return null;
            }

            foreach (ITextChannel channel in server.GetTextChannelsAsync().Result)
            {
                if (channel.Id == id)
                    return channel;
            }
            return null;
        }
    }
}
