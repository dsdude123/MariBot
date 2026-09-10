using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Models.Config;

namespace MariBot.Core.Services
{
    /// <summary>
    /// The set of workers Core currently believes exists, and the rules for
    /// getting into and out of it.
    /// </summary>
    /// <remarks>
    /// Membership is entirely driven by the workers themselves: they register on
    /// startup and heartbeat to stay, and one that goes quiet is shown Offline
    /// and then dropped. Nothing is read from a file, so a machine that no
    /// longer exists cannot still be in the list after a reboot.
    /// <para>
    /// Every method locks. Registrations and heartbeats arrive on request
    /// threads while the dispatcher runs on a timer, so this is genuinely
    /// concurrent even with a single worker.
    /// </para>
    /// </remarks>
    public class WorkerRegistry
    {
        private readonly ILogger<WorkerRegistry> logger;
        private readonly WorkerSettings settings;
        private readonly List<Worker> workers = new();
        private readonly object gate = new();

        public WorkerRegistry(ILogger<WorkerRegistry> logger, WorkerSettings settings)
        {
            this.logger = logger;
            this.settings = settings;
        }

        /// <summary>
        /// The registry as it stands. A copy of the list, so callers — the admin
        /// page especially — can enumerate it without racing a registration.
        /// </summary>
        public IReadOnlyList<Worker> Snapshot()
        {
            lock (gate)
            {
                return workers.ToArray();
            }
        }

        /// <summary>
        /// Registers a worker, or refreshes the registration of one already
        /// known at the same endpoint.
        /// </summary>
        /// <remarks>
        /// Keyed on endpoint rather than on a worker-generated id: a worker that
        /// restarts has forgotten whatever id it had, and keying on the address
        /// Core dials is what guarantees an endpoint appears exactly once. A
        /// re-registration therefore means "this worker just restarted" —
        /// in-flight job forgotten, breaker reset, counters cleared.
        /// </remarks>
        public Worker Register(WorkerRegistration request, WorkerCapability[] capabilities, DateTimeOffset now)
        {
            var endpoint = NormaliseEndpoint(request.Endpoint!);

            lock (gate)
            {
                var existing = workers.FirstOrDefault(w =>
                    string.Equals(w.Endpoint, endpoint, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    logger.LogInformation("Worker {Endpoint} re-registered as {Name}; resetting its state.",
                        endpoint, request.Name);

                    existing.Name = request.Name ?? existing.Name;
                    existing.Capabilities = capabilities;
                    existing.RegisteredAt = now;
                    existing.LastHeartbeat = now;
                    existing.CurrentJob = Guid.Empty;
                    existing.CurrentJobStartedAt = null;
                    existing.CurrentJobDeadline = null;
                    existing.Breaker = new CircuitBreaker();
                    existing.JobsCompleted = 0;
                    existing.JobsFailed = 0;

                    // A held worker stays held across a restart. Holding means
                    // "I am using this machine for something else", and that is
                    // not over just because the worker process bounced.
                    if (existing.Status != WorkerStatus.Held)
                    {
                        existing.Status = WorkerStatus.Ready;
                    }

                    return existing;
                }

                var worker = new Worker
                {
                    Id = Guid.NewGuid(),
                    Name = string.IsNullOrWhiteSpace(request.Name) ? endpoint : request.Name!,
                    Endpoint = endpoint,
                    Capabilities = capabilities,
                    Status = WorkerStatus.Ready,
                    RegisteredAt = now,
                    LastHeartbeat = now
                };

                workers.Add(worker);
                logger.LogInformation("Worker {Name} registered at {Endpoint} with capabilities {Capabilities}.",
                    worker.Name, worker.Endpoint, string.Join(", ", capabilities));

                return worker;
            }
        }

        /// <summary>
        /// Records a check-in, bringing a worker previously marked Offline back
        /// into rotation.
        /// </summary>
        /// <returns>False if the worker is not registered — it should register again.</returns>
        public bool Heartbeat(Guid id, DateTimeOffset now)
        {
            lock (gate)
            {
                var worker = workers.FirstOrDefault(w => w.Id == id);
                if (worker == null)
                {
                    return false;
                }

                worker.LastHeartbeat = now;

                if (worker.Status == WorkerStatus.Offline)
                {
                    logger.LogInformation("Worker {Name} is heartbeating again; back in rotation.", worker.Name);
                    worker.Status = WorkerStatus.Ready;
                }

                return true;
            }
        }

        /// <summary>
        /// Removes a worker that is shutting down cleanly, so it goes away
        /// immediately rather than after the eviction timeout.
        /// </summary>
        public bool Deregister(Guid id)
        {
            lock (gate)
            {
                var worker = workers.FirstOrDefault(w => w.Id == id);
                if (worker == null)
                {
                    return false;
                }

                workers.Remove(worker);
                logger.LogInformation("Worker {Name} deregistered.", worker.Name);
                return true;
            }
        }

        /// <summary>
        /// Ages out workers that have stopped checking in: Offline first, then
        /// removed.
        /// </summary>
        /// <returns>Workers evicted by this sweep, so their in-flight jobs can be failed.</returns>
        public IReadOnlyList<Worker> SweepStale(DateTimeOffset now)
        {
            var evicted = new List<Worker>();

            lock (gate)
            {
                foreach (var worker in workers.ToArray())
                {
                    var silence = now - worker.LastHeartbeat;

                    if (silence >= settings.EvictAfter)
                    {
                        workers.Remove(worker);
                        evicted.Add(worker);
                        logger.LogWarning("Worker {Name} has not checked in for {Silence}; evicting it.",
                            worker.Name, silence);
                        continue;
                    }

                    if (silence >= settings.OfflineAfter && worker.Status != WorkerStatus.Offline
                                                         && worker.Status != WorkerStatus.Held)
                    {
                        logger.LogWarning("Worker {Name} missed its heartbeat; marking offline.", worker.Name);
                        worker.Status = WorkerStatus.Offline;
                    }
                }
            }

            return evicted;
        }

        /// <summary>
        /// Takes a worker out of rotation by hand, for when the operator wants
        /// the machine for something else. Survives the worker restarting.
        /// </summary>
        public bool Hold(Guid id) => SetStatus(id, WorkerStatus.Held);

        /// <summary>
        /// Puts a held worker back into rotation, clearing any breaker state so
        /// it does not come back still tripped.
        /// </summary>
        public bool Ready(Guid id)
        {
            lock (gate)
            {
                var worker = workers.FirstOrDefault(w => w.Id == id);
                if (worker == null)
                {
                    return false;
                }

                logger.LogInformation("Worker {Name} returned to ready by hand.", worker.Name);
                worker.Status = WorkerStatus.Ready;
                worker.Breaker.RecordSuccess();
                return true;
            }
        }

        /// <summary>
        /// Whether some worker could run this capability eventually — idle,
        /// busy, or waiting out a cooldown, but not held, offline or absent.
        /// Answers "accept this request or turn it away".
        /// </summary>
        public bool AnyCouldServe(WorkerCapability capability, DateTimeOffset now)
        {
            lock (gate)
            {
                return workers.Any(w => w.CouldServe(capability, now, settings.BreakerOptions));
            }
        }

        /// <summary>
        /// Claims an idle worker for a job, taking its breaker's permission with
        /// it. The worker is marked Working before this returns, so two
        /// dispatches cannot claim the same one.
        /// </summary>
        /// <returns>The claimed worker, or null if none is currently available.</returns>
        public Worker? TryClaim(WorkerCapability capability, WorkerJob job, TimeSpan timeout, DateTimeOffset now)
        {
            lock (gate)
            {
                var candidate = workers
                    .Where(w => w.Status == WorkerStatus.Ready && w.HasCapability(capability))
                    // A healthy worker before one that is only being trialled,
                    // then whichever has been idle longest.
                    .OrderBy(w => w.Breaker.State == CircuitState.Closed ? 0 : 1)
                    .ThenBy(w => w.CurrentJobStartedAt ?? DateTimeOffset.MinValue)
                    .FirstOrDefault(w => w.Breaker.TryAcquire(now, settings.BreakerOptions));

                if (candidate == null)
                {
                    return null;
                }

                candidate.Status = WorkerStatus.Working;
                candidate.CurrentJob = job.Id;
                candidate.CurrentJobStartedAt = now;
                candidate.CurrentJobDeadline = now + timeout;
                return candidate;
            }
        }

        /// <summary>
        /// Releases a worker after a job ends, recording the outcome against its
        /// breaker and counters.
        /// </summary>
        /// <remarks>
        /// <paramref name="workerAtFault"/> is the distinction that makes the
        /// breaker useful: a job that failed because the user sent a broken
        /// image says nothing about the worker, and must not count towards
        /// taking it out of rotation.
        /// </remarks>
        public void Release(Guid workerId, Guid jobId, bool workerAtFault, DateTimeOffset now)
        {
            lock (gate)
            {
                var worker = workers.FirstOrDefault(w => w.Id == workerId);

                // Only clear the slot if it still holds this job. A late result
                // from a job we already timed out must not release the worker
                // from whatever it picked up afterwards.
                if (worker == null || worker.CurrentJob != jobId)
                {
                    return;
                }

                worker.CurrentJob = Guid.Empty;
                worker.CurrentJobStartedAt = null;
                worker.CurrentJobDeadline = null;

                if (workerAtFault)
                {
                    worker.JobsFailed++;
                    worker.Breaker.RecordFailure(now, settings.BreakerOptions);

                    if (worker.Breaker.State == CircuitState.Open)
                    {
                        logger.LogWarning(
                            "Worker {Name} failed {Failures} jobs in a row; opening its breaker for {Cooldown}.",
                            worker.Name, worker.Breaker.ConsecutiveFailures, settings.BreakerOptions.Cooldown);
                    }
                }
                else
                {
                    worker.JobsCompleted++;
                    worker.Breaker.RecordSuccess();
                }

                if (worker.Status == WorkerStatus.Working)
                {
                    worker.Status = WorkerStatus.Ready;
                }
            }
        }

        /// <summary>
        /// Workers whose current job has run past its deadline.
        /// </summary>
        public IReadOnlyList<Worker> FindOverdue(DateTimeOffset now)
        {
            lock (gate)
            {
                return workers
                    .Where(w => w.CurrentJob != Guid.Empty
                                && w.CurrentJobDeadline.HasValue
                                && now >= w.CurrentJobDeadline.Value)
                    .ToArray();
            }
        }

        public Worker? FindByJob(Guid jobId)
        {
            lock (gate)
            {
                return workers.FirstOrDefault(w => w.CurrentJob == jobId);
            }
        }

        public Worker? FindById(Guid id)
        {
            lock (gate)
            {
                return workers.FirstOrDefault(w => w.Id == id);
            }
        }

        private bool SetStatus(Guid id, WorkerStatus status)
        {
            lock (gate)
            {
                var worker = workers.FirstOrDefault(w => w.Id == id);
                if (worker == null)
                {
                    return false;
                }

                logger.LogInformation("Worker {Name} set to {Status}.", worker.Name, status);
                worker.Status = status;
                return true;
            }
        }

        /// <summary>
        /// Trims a trailing slash so the same worker advertising "http://host:8092"
        /// and "http://host:8092/" is one entry rather than two.
        /// </summary>
        private static string NormaliseEndpoint(string endpoint) => endpoint.Trim().TrimEnd('/');
    }
}
