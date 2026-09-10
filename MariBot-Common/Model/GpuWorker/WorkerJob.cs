namespace MariBot.Common.Model.GpuWorker
{
    public class WorkerJob
    {
        public Guid Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public Command Command { get; set; }
        public string? ReturnHost { get; set; }
        public byte[]? SourceImage { get; set; }
        public string? SourceText { get; set; }
        public JobResult? Result { get; set; }
        public int? ImageSelector { get; set; }

        /// <summary>
        /// Queue this job waits in. Set by Core from the command's profile at
        /// enqueue; carried on the job so a worker can log what it is running.
        /// </summary>
        public JobPriority Priority { get; set; } = JobPriority.Normal;

        /// <summary>
        /// How long the worker may spend on this job before giving up on it.
        /// Core decides and sends it, so both ends abandon at the same point
        /// rather than each holding its own opinion. Zero means the worker's
        /// own default.
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>When Core accepted the request. Used for queue-wait metrics.</summary>
        public DateTimeOffset EnqueuedAt { get; set; }

        /// <summary>When Core handed the job to a worker. Null while queued.</summary>
        public DateTimeOffset? DispatchedAt { get; set; }
    }
}
