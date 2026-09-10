using MariBot.Common.Model.GpuWorker;

namespace MariBot.Core.Models
{
    /// <summary>
    /// Job metrics rolled up over a window: overall, per command, and per worker.
    /// </summary>
    /// <remarks>
    /// The aggregation is a pure function of the rows it is given, which is what
    /// makes it testable without a database.
    /// </remarks>
    public class JobMetricsReport
    {
        public TimeSpan Window { get; init; }

        public DateTimeOffset GeneratedAt { get; init; }

        public int TotalJobs { get; init; }

        public int Completed { get; init; }

        public int Failed { get; init; }

        public int TimedOut { get; init; }

        public int Dropped { get; init; }

        public double SuccessRate => TotalJobs == 0 ? 0 : (double)Completed / TotalJobs;

        public IReadOnlyList<CommandStats> Commands { get; init; } = Array.Empty<CommandStats>();

        public IReadOnlyList<WorkerStats> Workers { get; init; } = Array.Empty<WorkerStats>();

        public static JobMetricsReport Build(IReadOnlyList<JobMetric> metrics, TimeSpan window, DateTimeOffset now)
        {
            var commands = metrics
                .GroupBy(m => m.Command)
                .Select(g => new CommandStats
                {
                    Command = g.Key,
                    Priority = g.First().Priority,
                    Total = g.Count(),
                    Completed = g.Count(m => m.Outcome == JobOutcome.Completed),
                    Failed = g.Count(m => m.Outcome == JobOutcome.DispatchFailed),
                    TimedOut = g.Count(m => m.Outcome == JobOutcome.TimedOut),
                    Dropped = g.Count(m => m.Outcome == JobOutcome.Dropped),
                    // Only jobs that actually ran carry a meaningful duration;
                    // averaging a dropped job's zero in would flatter every
                    // command that fails a lot.
                    MedianRunMs = Percentile(RunDurations(g), 0.50),
                    P95RunMs = Percentile(RunDurations(g), 0.95),
                    MedianQueuedMs = Percentile(g.Select(m => m.QueuedMs).ToArray(), 0.50)
                })
                .OrderByDescending(c => c.Total)
                .ThenBy(c => c.Command.ToString(), StringComparer.Ordinal)
                .ToArray();

            var workers = metrics
                .Where(m => m.WorkerId.HasValue)
                .GroupBy(m => m.WorkerId!.Value)
                .Select(g => new WorkerStats
                {
                    WorkerId = g.Key,
                    WorkerName = g.Select(m => m.WorkerName).FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? "unknown",
                    Total = g.Count(),
                    Completed = g.Count(m => m.Outcome == JobOutcome.Completed),
                    Failed = g.Count(m => m.Outcome == JobOutcome.DispatchFailed),
                    TimedOut = g.Count(m => m.Outcome == JobOutcome.TimedOut),
                    MedianRunMs = Percentile(RunDurations(g), 0.50),
                    P95RunMs = Percentile(RunDurations(g), 0.95)
                })
                .OrderByDescending(w => w.Total)
                .ThenBy(w => w.WorkerName, StringComparer.Ordinal)
                .ToArray();

            return new JobMetricsReport
            {
                Window = window,
                GeneratedAt = now,
                TotalJobs = metrics.Count,
                Completed = metrics.Count(m => m.Outcome == JobOutcome.Completed),
                Failed = metrics.Count(m => m.Outcome == JobOutcome.DispatchFailed),
                TimedOut = metrics.Count(m => m.Outcome == JobOutcome.TimedOut),
                Dropped = metrics.Count(m => m.Outcome == JobOutcome.Dropped),
                Commands = commands,
                Workers = workers
            };
        }

        private static double[] RunDurations(IEnumerable<JobMetric> metrics) =>
            metrics.Where(m => m.RanMs > 0).Select(m => m.RanMs).ToArray();

        /// <summary>
        /// Nearest-rank percentile. Exact on the sample rather than interpolated:
        /// every value it reports is a duration some job really took.
        /// </summary>
        public static double Percentile(double[] values, double percentile)
        {
            if (values.Length == 0)
            {
                return 0;
            }

            var sorted = values.OrderBy(v => v).ToArray();
            var rank = (int)Math.Ceiling(percentile * sorted.Length);
            return sorted[Math.Clamp(rank - 1, 0, sorted.Length - 1)];
        }

        public class CommandStats
        {
            public Command Command { get; init; }
            public JobPriority Priority { get; init; }
            public int Total { get; init; }
            public int Completed { get; init; }
            public int Failed { get; init; }
            public int TimedOut { get; init; }
            public int Dropped { get; init; }
            public double MedianRunMs { get; init; }
            public double P95RunMs { get; init; }
            public double MedianQueuedMs { get; init; }
            public double SuccessRate => Total == 0 ? 0 : (double)Completed / Total;
        }

        public class WorkerStats
        {
            public Guid WorkerId { get; init; }
            public string WorkerName { get; init; } = string.Empty;
            public int Total { get; init; }
            public int Completed { get; init; }
            public int Failed { get; init; }
            public int TimedOut { get; init; }
            public double MedianRunMs { get; init; }
            public double P95RunMs { get; init; }
            public double SuccessRate => Total == 0 ? 0 : (double)Completed / Total;
        }
    }
}
