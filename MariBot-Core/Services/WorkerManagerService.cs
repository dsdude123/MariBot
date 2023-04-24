using System.Text;
using System.Text.Json.Serialization.Metadata;
using Discord;
using Discord.WebSocket;
using MariBot.Common.Model.GpuWorker;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace MariBot.Core.Services
{
    public class WorkerManagerService
    {
        private readonly ILogger<WorkerManagerService> logger;
        private readonly DiscordSocketClient discord;

        public List<Worker> Workers { get; set; }
        public Dictionary<Guid, ulong> AcceptanceNotifications { get; set; }
        private List<WorkerJob> pendingJobs;
        private Timer jobDispatchTimer;

        public WorkerManagerService(ILogger<WorkerManagerService> logger, DiscordSocketClient discord)
        {
            this.logger = logger;
            this.discord = discord;
            AcceptanceNotifications = new Dictionary<Guid, ulong>();

            Workers = JsonConvert.DeserializeObject<List<Worker>>(File.ReadAllText("worker-config.json"));
            foreach (var worker in Workers)
            {
                try
                {
                    var http = new HttpClient();
                    http.BaseAddress = new Uri(worker.Endpoint);
                    http.GetAsync("/api/worker");
                    worker.Status = WorkerStatus.Ready;
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Worker {} failed health check. Marking offline.", worker.Endpoint);
                    worker.Status = WorkerStatus.Offline;
                }
            }

            pendingJobs = new List<WorkerJob>();

            jobDispatchTimer = new Timer(1000);
            jobDispatchTimer.Elapsed += JobDispatchTimer_Elapsed;
            jobDispatchTimer.AutoReset = false;
            jobDispatchTimer.Enabled = true;
            
        }

        /// <summary>
        /// Adds a new job for workers to work on.
        /// </summary>
        /// <param name="job">Job to work on</param>
        /// <returns>Status message</returns>
        public Tuple<string, Guid?> EnqueueJob(WorkerJob job)
        {
            logger.LogInformation("Incoming worker request {} for {}", job.Id, job.Command.ToString());
            
            var neededCapability = CommandCapabilityMapping.NeededCapabilityDictionary[job.Command];
            logger.LogInformation("Job {} - Capability {} needed.", job.Id, neededCapability.ToString());
            var workerMatch = Workers.Any(w =>
                w.Capabilities.Contains(neededCapability) && ((w.Status != WorkerStatus.Held) && (w.Status != WorkerStatus.Offline)));

            if (workerMatch)
            {
                pendingJobs.Add(job);
                return new Tuple<string, Guid?>("Your request was accepted.", job.Id);
            }
            else
            {
                logger.LogWarning("Job {} - No workers are available that can satisfy this request.", job.Id);
                return new Tuple<string, Guid?>("No workers are available that can satisfy this request. Try again later.", null);
            }
        }

        /// <summary>
        /// Handles a returned result from a worker
        /// </summary>
        /// <param name="job">Completed job</param>
        public void ReturnResult(WorkerJob job)
        {
            if (job.Result != null)
            {
                IGuild guild = FindServer(job.GuildId);
                ITextChannel channel = FindTextChannel(guild, job.ChannelId);

                if (job.Result.FileName != null && job.Result.Data != null)
                {
                    try
                    {
                        channel.SendFileAsync(new MemoryStream(job.Result.Data), job.Result.FileName,
                            job.Result.Message, messageReference: new MessageReference(job.MessageId));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Failed to send result for job {}. {}", job.Id, ex.Message);
                    }
                }
                else
                {
                    channel.SendMessageAsync(job.Result.Message, messageReference: new MessageReference(job.MessageId));
                }

                try
                {
                    var notificationToDelete = AcceptanceNotifications[job.Id];
                    channel.DeleteMessagesAsync(new[] { notificationToDelete });
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to clean up acceptance message for job {}. {}", job.Id, ex.Message);
                }
            }

            var assignedWorker = Workers.Find(w => w.CurrentJob.Equals(job.Id));
            if (assignedWorker != null)
            {
                assignedWorker.CurrentJob = Guid.Empty;
                if (assignedWorker.Status != WorkerStatus.Held) assignedWorker.Status = WorkerStatus.Ready;
            }
        }

        /// <summary>
        /// Holds a worker with the matching endpoint.
        /// </summary>
        /// <param name="endpoint">Worker endpoint</param>
        public void HoldWorker(string endpoint)
        {
            logger.LogInformation("Going to hold worker {}", endpoint);
            foreach (var worker in Workers)
            {
                if (worker.Endpoint.Equals(endpoint))
                {
                    worker.Status = WorkerStatus.Held;
                    return;
                }
            }
        }

        /// <summary>
        /// Puts worker with the matching endpoint in ready status.
        /// </summary>
        /// <param name="endpoint">Worker endpoint</param>
        public void ReadyWorker(string endpoint)
        {
            logger.LogInformation("Going to ready worker {}", endpoint);
            foreach (var worker in Workers)
            {
                if (worker.Endpoint.Equals(endpoint))
                {
                    worker.Status = WorkerStatus.Ready;
                    return;
                }
            }
        }

        private async void JobDispatchTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (pendingJobs.Count > 0)
            {
                for (var i = 0; i < pendingJobs.Count; i++)
                {
                    var job = pendingJobs[i];
                    var neededCapability = CommandCapabilityMapping.NeededCapabilityDictionary[job.Command];

                    var availableWorker = Workers.FirstOrDefault(w =>
                        w.Capabilities.Contains(neededCapability) && w.Status.Equals(WorkerStatus.Ready), null);
                    var busyWorker = Workers.FirstOrDefault(w =>
                        w.Capabilities.Contains(neededCapability) && w.Status.Equals(WorkerStatus.Working), null);
                    var unavailableWorker = Workers.FirstOrDefault(w =>
                        w.Capabilities.Contains(neededCapability) && (w.Status.Equals(WorkerStatus.Held) || w.Status.Equals(WorkerStatus.Offline)), null);

                    if (availableWorker != null)
                    {
                        logger.LogInformation("Job {} is being assigned to {}", job.Id, availableWorker.Endpoint);
                        var jsonString = JsonConvert.SerializeObject(job);
                        try
                        {
                            var http = new HttpClient();
                            http.BaseAddress = new Uri(availableWorker.Endpoint);
                            var response = await http.PostAsync("/api/worker", new StringContent(jsonString, Encoding.UTF8, "application/json"));
                            response.EnsureSuccessStatusCode();
                            availableWorker.Status = WorkerStatus.Working;
                            availableWorker.CurrentJob = job.Id;
                            pendingJobs.RemoveAt(i);
                            jobDispatchTimer.Enabled = true;
                            return;
                        }
                        catch (Exception exception)
                        {
                            logger.LogCritical("Worker at {} failed to accept job. Marking as offline. {}", availableWorker.Endpoint, exception.Message);
                            availableWorker.Status = WorkerStatus.Offline;
                            job.Result = new JobResult
                            {
                                Message = "An error occurred while sending your request to a worker."
                            };
                            ReturnResult(job);
                            pendingJobs.RemoveAt(i);
                            jobDispatchTimer.Enabled = true;
                            return;
                        }
                    }

                    if (busyWorker == null && unavailableWorker != null)
                    {
                        // All workers that can handle this request are out of service
                        logger.LogWarning("Job {} is being dropped due to all possible workers going out of service.", job.Id);
                        job.Result = new JobResult
                        {
                            Message = "The required worker was marked out of service before your request could be processed. Please try again later."
                        };
                        ReturnResult(job);
                        pendingJobs.RemoveAt(i);
                        jobDispatchTimer.Enabled = true;
                        return;
                    }
                }
            }

            jobDispatchTimer.Enabled = true;
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
            foreach (ITextChannel channel in server.GetTextChannelsAsync().Result)
            {
                if (channel.Id == id)
                    return channel;
            }
            return null;
        }

    }
}
