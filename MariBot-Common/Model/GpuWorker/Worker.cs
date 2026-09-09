namespace MariBot.Common.Model.GpuWorker
{
    /// <summary>
    /// A worker Core knows about.
    /// </summary>
    /// <remarks>
    /// Entries are created by workers registering themselves and removed when
    /// they stop heartbeating; nothing here is read from a file any more. That
    /// is the whole point — a worker-config.json listing a machine that has
    /// since been decommissioned produced a permanently Offline entry and a
    /// stream of failed dispatches after every reboot.
    /// </remarks>
    public class Worker
    {
        /// <summary>Assigned by Core at registration. Identifies the worker in logs, metrics and the admin UI.</summary>
        public Guid Id { get; set; }

        /// <summary>Self-reported display name, usually the machine or container name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Base address Core posts jobs to, as the worker advertised it.</summary>
        public string Endpoint { get; set; } = string.Empty;

        public WorkerCapability[] Capabilities { get; set; } = Array.Empty<WorkerCapability>();

        public WorkerStatus Status { get; set; }

        public Guid CurrentJob { get; set; }

        public DateTimeOffset? CurrentJobStartedAt { get; set; }

        /// <summary>
        /// When the running job is abandoned. Null when idle.
        /// </summary>
        public DateTimeOffset? CurrentJobDeadline { get; set; }

        public DateTimeOffset RegisteredAt { get; set; }

        /// <summary>
        /// Last heartbeat received. A worker that goes quiet is marked Offline
        /// and then evicted; this is what makes a stale entry impossible.
        /// </summary>
        public DateTimeOffset LastHeartbeat { get; set; }

        public CircuitBreaker Breaker { get; set; } = new();

        /// <summary>Jobs this worker has completed since registering. Reset when it re-registers.</summary>
        public int JobsCompleted { get; set; }

        /// <summary>Jobs this worker failed or let time out since registering.</summary>
        public int JobsFailed { get; set; }

        public bool HasCapability(WorkerCapability capability) =>
            Capabilities != null && Capabilities.Contains(capability);

        /// <summary>
        /// Whether this worker could take a job of the given capability at some
        /// point — idle or busy, but neither held, offline, nor breaker-open.
        /// Decides whether a request is accepted or refused outright.
        /// </summary>
        public bool CouldServe(WorkerCapability capability, DateTimeOffset now, CircuitBreakerOptions breakerOptions) =>
            HasCapability(capability)
            && Status != WorkerStatus.Held
            && Status != WorkerStatus.Offline
            && (Status == WorkerStatus.Working || Breaker.WouldAllow(now, breakerOptions));
    }
}
