using System;
using System.Collections.Generic;
using System.Linq;
using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Models;
using Xunit;

namespace MariBot.Core.Tests
{
    public class JobMetricsReportTests
    {
        private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        private static readonly Guid WorkerId = Guid.NewGuid();

        /// <summary>A job that reached a worker.</summary>
        private static JobMetric Metric(
            Command command = Command.DeepFry,
            JobOutcome outcome = JobOutcome.Completed,
            double ranMs = 100,
            double queuedMs = 10,
            Guid? workerId = null,
            string workerName = "box") => new()
        {
            Id = Guid.NewGuid(),
            Command = command,
            Priority = JobPriority.Normal,
            Outcome = outcome,
            WorkerId = workerId ?? WorkerId,
            WorkerName = workerName,
            RanMs = ranMs,
            QueuedMs = queuedMs,
            CompletedAt = Now
        };

        /// <summary>
        /// A job that never reached a worker, so it has neither a worker nor a
        /// runtime. A separate helper because defaulting the worker id is
        /// exactly the mistake this case is here to catch.
        /// </summary>
        private static JobMetric Dropped(Command command = Command.DeepFry) => new()
        {
            Id = Guid.NewGuid(),
            Command = command,
            Priority = JobPriority.Normal,
            Outcome = JobOutcome.Dropped,
            WorkerId = null,
            WorkerName = null,
            RanMs = 0,
            QueuedMs = 10,
            CompletedAt = Now
        };

        [Fact]
        public void CountsEachOutcomeSeparately()
        {
            var report = JobMetricsReport.Build(new[]
            {
                Metric(),
                Metric(outcome: JobOutcome.DispatchFailed),
                Metric(outcome: JobOutcome.TimedOut),
                Dropped()
            }, TimeSpan.FromHours(1), Now);

            Assert.Equal(4, report.TotalJobs);
            Assert.Equal(1, report.Completed);
            Assert.Equal(1, report.Failed);
            Assert.Equal(1, report.TimedOut);
            Assert.Equal(1, report.Dropped);
            Assert.Equal(0.25, report.SuccessRate);
        }

        [Fact]
        public void RanksCommandsByHowMuchTheyAreUsed()
        {
            var metrics = Enumerable.Repeat(Metric(Command.DeepFry), 5)
                .Concat(Enumerable.Repeat(Metric(Command.Trump), 9))
                .Concat(Enumerable.Repeat(Metric(Command.Adidas), 2))
                .ToArray();

            var report = JobMetricsReport.Build(metrics, TimeSpan.FromHours(1), Now);

            Assert.Equal(new[] { Command.Trump, Command.DeepFry, Command.Adidas },
                report.Commands.Select(c => c.Command).ToArray());
            Assert.Equal(9, report.Commands.First().Total);
        }

        [Fact]
        public void DurationsOnlyCountJobsThatActuallyRan()
        {
            var report = JobMetricsReport.Build(new[]
            {
                Metric(ranMs: 100),
                Metric(ranMs: 200),
                // Never dispatched, so it has no runtime. Averaging its zero in
                // would make a command that fails a lot look fast.
                Dropped()
            }, TimeSpan.FromHours(1), Now);

            var command = Assert.Single(report.Commands);
            Assert.Equal(3, command.Total);
            Assert.Equal(100, command.MedianRunMs);
            Assert.Equal(200, command.P95RunMs);
        }

        [Fact]
        public void GroupsByWorkerAndIgnoresJobsThatNeverReachedOne()
        {
            var otherWorker = Guid.NewGuid();
            var report = JobMetricsReport.Build(new[]
            {
                Metric(workerId: WorkerId, workerName: "alpha"),
                Metric(workerId: WorkerId, workerName: "alpha", outcome: JobOutcome.TimedOut),
                Metric(workerId: otherWorker, workerName: "beta"),
                Dropped()
            }, TimeSpan.FromHours(1), Now);

            Assert.Equal(2, report.Workers.Count);
            var alpha = report.Workers.Single(w => w.WorkerName == "alpha");
            Assert.Equal(2, alpha.Total);
            Assert.Equal(1, alpha.Completed);
            Assert.Equal(1, alpha.TimedOut);
            Assert.Equal(0.5, alpha.SuccessRate);
        }

        [Fact]
        public void EmptyWindowProducesAnEmptyReportRatherThanDividingByZero()
        {
            var report = JobMetricsReport.Build(Array.Empty<JobMetric>(), TimeSpan.FromHours(1), Now);

            Assert.Equal(0, report.TotalJobs);
            Assert.Equal(0, report.SuccessRate);
            Assert.Empty(report.Commands);
            Assert.Empty(report.Workers);
        }

        [Theory]
        [InlineData(0.5, 3)]
        [InlineData(0.95, 5)]
        [InlineData(1.0, 5)]
        public void PercentileIsNearestRankSoEveryValueIsARealMeasurement(double percentile, double expected)
        {
            var values = new double[] { 5, 1, 3, 2, 4 };

            Assert.Equal(expected, JobMetricsReport.Percentile(values, percentile));
        }

        [Fact]
        public void PercentileOfNothingIsZero()
        {
            Assert.Equal(0, JobMetricsReport.Percentile(Array.Empty<double>(), 0.95));
        }
    }
}
