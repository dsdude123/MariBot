using System;
using System.Linq;
using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Models.Config;
using MariBot.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MariBot.Core.Tests
{
    public class WorkerRegistryTests
    {
        private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private static WorkerSettings Settings() => new()
        {
            HeartbeatSeconds = 10,
            OfflineAfterMissedHeartbeats = 3,
            EvictAfterMissedHeartbeats = 6,
            CircuitBreakerFailureThreshold = 2,
            CircuitBreakerCooldownSeconds = 60
        };

        private static WorkerRegistry Registry(WorkerSettings settings) =>
            new(NullLogger<WorkerRegistry>.Instance, settings);

        private static WorkerRegistration Registration(string endpoint, string name = "worker") => new()
        {
            Name = name,
            Endpoint = endpoint,
            Capabilities = new[] { "CPU" }
        };

        private static WorkerJob Job(Command command = Command.DeepFry) => new()
        {
            Id = Guid.NewGuid(),
            Command = command
        };

        [Fact]
        public void RegisteringAddsAReadyWorker()
        {
            var registry = Registry(Settings());

            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);

            Assert.NotEqual(Guid.Empty, worker.Id);
            Assert.Equal(WorkerStatus.Ready, worker.Status);
            Assert.Single(registry.Snapshot());
        }

        [Fact]
        public void ReRegisteringTheSameEndpointReplacesRatherThanDuplicates()
        {
            var registry = Registry(Settings());
            var first = registry.Register(Registration("http://box:8092", "old"), new[] { WorkerCapability.CPU }, T0);

            var second = registry.Register(Registration("http://box:8092/", "new"),
                new[] { WorkerCapability.CPU, WorkerCapability.ConsumerGPU }, T0.AddMinutes(5));

            // One endpoint, one entry — including across the trailing slash.
            // This is what stops a rebooting worker piling up stale rows.
            Assert.Single(registry.Snapshot());
            Assert.Equal(first.Id, second.Id);
            Assert.Equal("new", second.Name);
            Assert.Contains(WorkerCapability.ConsumerGPU, second.Capabilities);
        }

        [Fact]
        public void ReRegisteringClearsATrippedBreakerAndAnyInFlightJob()
        {
            var settings = Settings();
            var registry = Registry(settings);
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);
            var job = Job();
            registry.TryClaim(WorkerCapability.CPU, job, TimeSpan.FromSeconds(30), T0);
            registry.Release(worker.Id, job.Id, workerAtFault: true, T0);
            registry.Release(worker.Id, job.Id, workerAtFault: true, T0);

            var again = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0.AddMinutes(1));

            // A worker that restarted is a fresh worker; carrying its old
            // failures over would keep a recovered box out of rotation.
            Assert.Equal(CircuitState.Closed, again.Breaker.State);
            Assert.Equal(Guid.Empty, again.CurrentJob);
            Assert.Equal(WorkerStatus.Ready, again.Status);
        }

        [Fact]
        public void AHeldWorkerStaysHeldAcrossARestart()
        {
            var registry = Registry(Settings());
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);
            registry.Hold(worker.Id);

            var again = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0.AddMinutes(1));

            // Holding means "I am using this machine", which a process bounce
            // does not end.
            Assert.Equal(WorkerStatus.Held, again.Status);
        }

        [Fact]
        public void AQuietWorkerGoesOfflineThenIsEvicted()
        {
            var settings = Settings();
            var registry = Registry(settings);
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);

            registry.SweepStale(T0.AddSeconds(31));
            Assert.Equal(WorkerStatus.Offline, registry.FindById(worker.Id)!.Status);

            var evicted = registry.SweepStale(T0.AddSeconds(61));

            // The stale entry does not linger: this is the reboot problem gone.
            Assert.Equal(worker.Id, Assert.Single(evicted).Id);
            Assert.Empty(registry.Snapshot());
        }

        [Fact]
        public void HeartbeatingBringsAnOfflineWorkerBack()
        {
            var registry = Registry(Settings());
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);
            registry.SweepStale(T0.AddSeconds(31));

            Assert.True(registry.Heartbeat(worker.Id, T0.AddSeconds(35)));

            Assert.Equal(WorkerStatus.Ready, registry.FindById(worker.Id)!.Status);
        }

        [Fact]
        public void HeartbeatFromAnUnknownWorkerIsRefusedSoItReRegisters()
        {
            var registry = Registry(Settings());

            Assert.False(registry.Heartbeat(Guid.NewGuid(), T0));
        }

        [Fact]
        public void ClaimingMarksTheWorkerBusySoASecondJobCannotTakeIt()
        {
            var registry = Registry(Settings());
            registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);

            var first = registry.TryClaim(WorkerCapability.CPU, Job(), TimeSpan.FromSeconds(30), T0);
            var second = registry.TryClaim(WorkerCapability.CPU, Job(), TimeSpan.FromSeconds(30), T0);

            Assert.NotNull(first);
            Assert.Null(second);
        }

        [Fact]
        public void ClaimingIgnoresWorkersWithoutTheCapability()
        {
            var registry = Registry(Settings());
            registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);

            Assert.Null(registry.TryClaim(WorkerCapability.DatacenterGPU, Job(), TimeSpan.FromSeconds(30), T0));
        }

        [Fact]
        public void ClaimingSkipsHeldWorkers()
        {
            var registry = Registry(Settings());
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);
            registry.Hold(worker.Id);

            Assert.Null(registry.TryClaim(WorkerCapability.CPU, Job(), TimeSpan.FromSeconds(30), T0));
        }

        [Fact]
        public void AWorkerWithAnOpenBreakerIsSkippedUntilItsCooldownPasses()
        {
            var settings = Settings();
            var registry = Registry(settings);
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);

            FailJobs(registry, worker.Id, settings.CircuitBreakerFailureThreshold, T0);

            Assert.Null(registry.TryClaim(WorkerCapability.CPU, Job(), TimeSpan.FromSeconds(30), T0.AddSeconds(30)));
            Assert.NotNull(registry.TryClaim(WorkerCapability.CPU, Job(), TimeSpan.FromSeconds(30), T0.AddSeconds(61)));
        }

        [Fact]
        public void AHealthyWorkerIsPreferredOverOneOnlyBeingTrialled()
        {
            var settings = Settings();
            var registry = Registry(settings);
            var sick = registry.Register(Registration("http://sick:8092", "sick"), new[] { WorkerCapability.CPU }, T0);
            registry.Register(Registration("http://healthy:8092", "healthy"), new[] { WorkerCapability.CPU }, T0);
            FailJobs(registry, sick.Id, settings.CircuitBreakerFailureThreshold, T0);

            var claimed = registry.TryClaim(WorkerCapability.CPU, Job(), TimeSpan.FromSeconds(30), T0.AddSeconds(61));

            // Real traffic goes to the worker we trust; the recovering one gets
            // a trial only when there is nothing better.
            Assert.Equal("healthy", claimed!.Name);
        }

        [Fact]
        public void ReleasingWithoutFaultRecordsASuccessAndFreesTheWorker()
        {
            var registry = Registry(Settings());
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);
            var job = Job();
            registry.TryClaim(WorkerCapability.CPU, job, TimeSpan.FromSeconds(30), T0);

            registry.Release(worker.Id, job.Id, workerAtFault: false, T0.AddSeconds(5));

            var after = registry.FindById(worker.Id)!;
            Assert.Equal(WorkerStatus.Ready, after.Status);
            Assert.Equal(Guid.Empty, after.CurrentJob);
            Assert.Equal(1, after.JobsCompleted);
            Assert.Equal(CircuitState.Closed, after.Breaker.State);
        }

        [Fact]
        public void ALateReleaseForAnAlreadyReplacedJobIsIgnored()
        {
            var registry = Registry(Settings());
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);
            var timedOut = Job();
            registry.TryClaim(WorkerCapability.CPU, timedOut, TimeSpan.FromSeconds(30), T0);
            registry.Release(worker.Id, timedOut.Id, workerAtFault: true, T0.AddSeconds(31));
            var next = Job();
            registry.TryClaim(WorkerCapability.CPU, next, TimeSpan.FromSeconds(30), T0.AddSeconds(32));

            // The abandoned job finally finishes and reports back.
            registry.Release(worker.Id, timedOut.Id, workerAtFault: false, T0.AddSeconds(40));

            // It must not release the worker from the job it is running now.
            var after = registry.FindById(worker.Id)!;
            Assert.Equal(next.Id, after.CurrentJob);
            Assert.Equal(WorkerStatus.Working, after.Status);
        }

        [Fact]
        public void OverdueJobsAreFoundOnceTheirDeadlinePasses()
        {
            var registry = Registry(Settings());
            registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);
            registry.TryClaim(WorkerCapability.CPU, Job(), TimeSpan.FromSeconds(30), T0);

            Assert.Empty(registry.FindOverdue(T0.AddSeconds(29)));
            Assert.Single(registry.FindOverdue(T0.AddSeconds(30)));
        }

        [Fact]
        public void AnyCouldServeCountsBusyWorkersButNotHeldOrOfflineOnes()
        {
            var settings = Settings();
            var registry = Registry(settings);
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);

            Assert.True(registry.AnyCouldServe(WorkerCapability.CPU, T0));

            registry.TryClaim(WorkerCapability.CPU, Job(), TimeSpan.FromSeconds(30), T0);
            // Busy is not unavailable — the job just waits its turn.
            Assert.True(registry.AnyCouldServe(WorkerCapability.CPU, T0));

            registry.Hold(worker.Id);
            Assert.False(registry.AnyCouldServe(WorkerCapability.CPU, T0));
        }

        [Fact]
        public void ReadyingByHandClearsATrippedBreaker()
        {
            var settings = Settings();
            var registry = Registry(settings);
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);
            FailJobs(registry, worker.Id, settings.CircuitBreakerFailureThreshold, T0);

            registry.Ready(worker.Id);

            var after = registry.FindById(worker.Id)!;
            Assert.Equal(CircuitState.Closed, after.Breaker.State);
            Assert.NotNull(registry.TryClaim(WorkerCapability.CPU, Job(), TimeSpan.FromSeconds(30), T0));
        }

        [Fact]
        public void DeregisteringRemovesTheWorkerImmediately()
        {
            var registry = Registry(Settings());
            var worker = registry.Register(Registration("http://box:8092"), new[] { WorkerCapability.CPU }, T0);

            Assert.True(registry.Deregister(worker.Id));
            Assert.Empty(registry.Snapshot());
            Assert.False(registry.Deregister(worker.Id));
        }

        private static void FailJobs(WorkerRegistry registry, Guid workerId, int count, DateTimeOffset at)
        {
            for (var i = 0; i < count; i++)
            {
                var job = new WorkerJob { Id = Guid.NewGuid(), Command = Command.DeepFry };
                registry.TryClaim(WorkerCapability.CPU, job, TimeSpan.FromSeconds(30), at);
                registry.Release(workerId, job.Id, workerAtFault: true, at);
            }
        }
    }
}
