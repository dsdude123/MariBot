using System;
using MariBot.Common.Model.GpuWorker;
using Xunit;

namespace MariBot.Common.Tests
{
    public class CircuitBreakerTests
    {
        private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private static CircuitBreakerOptions Options(int threshold = 3, int cooldownSeconds = 60) => new()
        {
            FailureThreshold = threshold,
            Cooldown = TimeSpan.FromSeconds(cooldownSeconds)
        };

        [Fact]
        public void StartsClosedAndDispatches()
        {
            var breaker = new CircuitBreaker();

            Assert.Equal(CircuitState.Closed, breaker.State);
            Assert.True(breaker.TryAcquire(T0, Options()));
        }

        [Fact]
        public void OneFailureDoesNotOpenTheBreaker()
        {
            var breaker = new CircuitBreaker();

            breaker.RecordFailure(T0, Options());

            // The whole point: a single bad job must not take a worker out of
            // rotation, which is what the old mark-offline-on-error did.
            Assert.Equal(CircuitState.Closed, breaker.State);
            Assert.True(breaker.TryAcquire(T0, Options()));
        }

        [Fact]
        public void OpensOnTheConfiguredRunOfFailures()
        {
            var breaker = new CircuitBreaker();
            var options = Options(threshold: 3);

            breaker.RecordFailure(T0, options);
            breaker.RecordFailure(T0, options);
            Assert.Equal(CircuitState.Closed, breaker.State);

            breaker.RecordFailure(T0, options);

            Assert.Equal(CircuitState.Open, breaker.State);
            Assert.Equal(1, breaker.Trips);
            Assert.False(breaker.TryAcquire(T0, options));
        }

        [Fact]
        public void SuccessResetsTheFailureRun()
        {
            var breaker = new CircuitBreaker();
            var options = Options(threshold: 3);

            breaker.RecordFailure(T0, options);
            breaker.RecordFailure(T0, options);
            breaker.RecordSuccess();
            breaker.RecordFailure(T0, options);
            breaker.RecordFailure(T0, options);

            Assert.Equal(CircuitState.Closed, breaker.State);
        }

        [Fact]
        public void StaysOpenUntilTheCooldownElapses()
        {
            var breaker = OpenBreaker(out var options);

            Assert.False(breaker.TryAcquire(T0.AddSeconds(59), options));
            Assert.Equal(CircuitState.Open, breaker.State);
        }

        [Fact]
        public void GoesHalfOpenAfterTheCooldownAndAllowsOneTrial()
        {
            var breaker = OpenBreaker(out var options);
            var later = T0.AddSeconds(60);

            Assert.True(breaker.TryAcquire(later, options));
            Assert.Equal(CircuitState.HalfOpen, breaker.State);

            // Only one job gets to find out whether the worker recovered.
            Assert.False(breaker.TryAcquire(later, options));
        }

        [Fact]
        public void SuccessfulTrialClosesTheBreaker()
        {
            var breaker = OpenBreaker(out var options);
            var later = T0.AddSeconds(60);
            breaker.TryAcquire(later, options);

            breaker.RecordSuccess();

            Assert.Equal(CircuitState.Closed, breaker.State);
            Assert.Equal(0, breaker.ConsecutiveFailures);
            Assert.True(breaker.TryAcquire(later, options));
        }

        [Fact]
        public void FailedTrialReopensImmediatelyWithoutWaitingForTheThreshold()
        {
            var breaker = OpenBreaker(out var options);
            var later = T0.AddSeconds(60);
            breaker.TryAcquire(later, options);

            breaker.RecordFailure(later, options);

            Assert.Equal(CircuitState.Open, breaker.State);
            Assert.Equal(2, breaker.Trips);
            // And the cooldown restarts from the failed trial, not the original trip.
            Assert.False(breaker.TryAcquire(later.AddSeconds(59), options));
            Assert.True(breaker.TryAcquire(later.AddSeconds(60), options));
        }

        [Fact]
        public void WouldAllowReportsDispatchabilityWithoutClaimingTheTrial()
        {
            var breaker = OpenBreaker(out var options);
            var later = T0.AddSeconds(60);

            Assert.False(breaker.WouldAllow(T0.AddSeconds(30), options));
            Assert.True(breaker.WouldAllow(later, options));

            // Asking must not have consumed the half-open trial.
            Assert.Equal(CircuitState.Open, breaker.State);
            Assert.True(breaker.TryAcquire(later, options));
        }

        [Fact]
        public void RepeatedFailuresWhileOpenDoNotInflateTheTripCount()
        {
            var breaker = OpenBreaker(out var options);

            breaker.RecordFailure(T0, options);
            breaker.RecordFailure(T0, options);

            Assert.Equal(1, breaker.Trips);
        }

        private static CircuitBreaker OpenBreaker(out CircuitBreakerOptions options)
        {
            options = Options(threshold: 3);
            var breaker = new CircuitBreaker();

            for (var i = 0; i < 3; i++)
            {
                breaker.RecordFailure(T0, options);
            }

            Assert.Equal(CircuitState.Open, breaker.State);
            return breaker;
        }
    }
}
