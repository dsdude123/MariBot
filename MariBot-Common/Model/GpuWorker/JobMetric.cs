namespace MariBot.Common.Model.GpuWorker
{
    /// <summary>
    /// One finished job, recorded for the metrics page.
    /// </summary>
    /// <remarks>
    /// Deliberately a flat row rather than pre-aggregated counters: the
    /// questions worth asking of this data later ("which command got slower
    /// after that change") are not ones you can ask of a counter.
    /// </remarks>
    public class JobMetric
    {
        public Guid Id { get; set; }

        public Command Command { get; set; }

        public JobPriority Priority { get; set; }

        public JobOutcome Outcome { get; set; }

        /// <summary>Worker that ran it, or null if it never got that far.</summary>
        public Guid? WorkerId { get; set; }

        public string? WorkerName { get; set; }

        public ulong GuildId { get; set; }

        /// <summary>Milliseconds spent waiting in the queue before dispatch.</summary>
        public double QueuedMs { get; set; }

        /// <summary>Milliseconds between dispatch and the result coming back. Zero if never dispatched.</summary>
        public double RanMs { get; set; }

        public DateTimeOffset CompletedAt { get; set; }

        public double TotalMs => QueuedMs + RanMs;
    }
}
