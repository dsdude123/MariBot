using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Models;
using MariBot.Core.Models.Config;
using Timer = System.Timers.Timer;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Records how every job went and rolls it up for the metrics page.
    /// </summary>
    /// <remarks>
    /// Rows are kept rather than counters, because the questions worth asking
    /// later — did this command get slower, which guild is generating the load —
    /// cannot be asked of a counter after the fact. LiteDB holds them so a
    /// restart, which this deployment does often, does not reset the picture.
    /// </remarks>
    public class JobMetricsService
    {
        private readonly ILogger<JobMetricsService> logger;
        private readonly DataService dataService;
        private readonly WorkerSettings settings;
        private readonly Timer pruneTimer;

        public JobMetricsService(ILogger<JobMetricsService> logger, DataService dataService, WorkerSettings settings)
        {
            this.logger = logger;
            this.dataService = dataService;
            this.settings = settings;

            pruneTimer = new Timer(TimeSpan.FromHours(6).TotalMilliseconds);
            pruneTimer.Elapsed += (_, _) => Prune();
            pruneTimer.AutoReset = true;
            pruneTimer.Enabled = true;
        }

        /// <summary>
        /// Records a finished job. Called for every terminal outcome, including
        /// the ones that never reached a worker — a job dropped because nothing
        /// could run it is exactly the kind of thing worth being able to count.
        /// </summary>
        public void Record(WorkerJob job, JobOutcome outcome, Worker? worker, DateTimeOffset now)
        {
            var dispatchedAt = job.DispatchedAt;
            var queuedMs = Math.Max(0, ((dispatchedAt ?? now) - job.EnqueuedAt).TotalMilliseconds);
            var ranMs = dispatchedAt.HasValue ? Math.Max(0, (now - dispatchedAt.Value).TotalMilliseconds) : 0;

            var metric = new JobMetric
            {
                Id = job.Id,
                Command = job.Command,
                Priority = job.Priority,
                Outcome = outcome,
                WorkerId = worker?.Id,
                WorkerName = worker?.Name,
                GuildId = job.GuildId,
                QueuedMs = queuedMs,
                RanMs = ranMs,
                CompletedAt = now
            };

            logger.LogInformation(
                "Job {JobId} {Command} finished as {Outcome} on {Worker} after {QueuedMs:F0}ms queued, {RanMs:F0}ms running.",
                job.Id, job.Command, outcome, worker?.Name ?? "no worker", queuedMs, ranMs);

            dataService.WriteJobMetric(metric);
        }

        /// <summary>
        /// Rolls up the last <paramref name="window"/> of jobs.
        /// </summary>
        public JobMetricsReport Report(TimeSpan window, DateTimeOffset now)
        {
            var since = now - window;
            var metrics = dataService.GetJobMetrics(since);
            return JobMetricsReport.Build(metrics, window, now);
        }

        /// <summary>
        /// Drops metrics past the retention window. Runs on a timer; retention
        /// exists because nothing else ever deletes from this collection.
        /// </summary>
        public int Prune()
        {
            var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromDays(Math.Max(1, settings.MetricsRetentionDays));
            var removed = dataService.PruneJobMetrics(cutoff);

            if (removed > 0)
            {
                logger.LogInformation("Pruned {Removed} job metrics older than {Cutoff}.", removed, cutoff);
            }

            return removed;
        }
    }
}
