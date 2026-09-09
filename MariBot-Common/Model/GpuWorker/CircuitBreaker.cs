namespace MariBot.Common.Model.GpuWorker
{
    /// <summary>
    /// Per-worker circuit breaker: closed while a worker is healthy, open after
    /// it fails repeatedly, and half-open once the cooldown has elapsed, where
    /// exactly one job is let through to find out whether it recovered.
    /// </summary>
    /// <remarks>
    /// The point is that a worker no longer falls out of rotation for a single
    /// error and stay out until somebody notices. One failure is noise; a run of
    /// them is a worker to stop sending work to; and recovery happens on its own.
    /// <para>
    /// Time is passed in rather than read, so the state machine is testable
    /// without waiting for a cooldown to elapse.
    /// </para>
    /// </remarks>
    public class CircuitBreaker
    {
        public CircuitState State { get; set; } = CircuitState.Closed;

        /// <summary>Failures since the last success. Reset by any success.</summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>When the breaker last opened, which is what the cooldown runs from.</summary>
        public DateTimeOffset? OpenedAt { get; set; }

        public DateTimeOffset? LastFailureAt { get; set; }

        /// <summary>How many times this breaker has opened. Only ever climbs; shown for monitoring.</summary>
        public int Trips { get; set; }

        /// <summary>
        /// True while the one job a half-open breaker permits is still running.
        /// Blocks a second trial from going out alongside it.
        /// </summary>
        public bool TrialInFlight { get; set; }

        /// <summary>
        /// Whether a job could dispatch now, without changing anything. Used to
        /// answer "will any worker ever take this job" at enqueue time.
        /// </summary>
        public bool WouldAllow(DateTimeOffset now, CircuitBreakerOptions options) => State switch
        {
            CircuitState.Closed => true,
            CircuitState.Open => OpenedAt.HasValue && now - OpenedAt.Value >= options.Cooldown,
            CircuitState.HalfOpen => !TrialInFlight,
            _ => false
        };

        /// <summary>
        /// Claims permission to dispatch one job, moving an expired open breaker
        /// to half-open and taking its single trial slot.
        /// </summary>
        /// <returns>True if the caller may dispatch.</returns>
        public bool TryAcquire(DateTimeOffset now, CircuitBreakerOptions options)
        {
            switch (State)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    if (!OpenedAt.HasValue || now - OpenedAt.Value < options.Cooldown)
                    {
                        return false;
                    }

                    State = CircuitState.HalfOpen;
                    TrialInFlight = true;
                    return true;

                case CircuitState.HalfOpen:
                    if (TrialInFlight)
                    {
                        return false;
                    }

                    TrialInFlight = true;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// A job came back from this worker. Closes the breaker outright: a
        /// worker that just did the work is healthy whatever it did before.
        /// </summary>
        public void RecordSuccess()
        {
            State = CircuitState.Closed;
            ConsecutiveFailures = 0;
            OpenedAt = null;
            TrialInFlight = false;
        }

        /// <summary>
        /// A job failed in a way that implicates the worker rather than the
        /// request — it refused the job, or never came back with a result.
        /// </summary>
        public void RecordFailure(DateTimeOffset now, CircuitBreakerOptions options)
        {
            ConsecutiveFailures++;
            LastFailureAt = now;

            // A failed trial re-opens immediately: the cooldown just proved
            // insufficient, so there is nothing to learn from a second try now.
            var shouldOpen = State == CircuitState.HalfOpen
                             || ConsecutiveFailures >= options.FailureThreshold;

            TrialInFlight = false;

            if (shouldOpen && State != CircuitState.Open)
            {
                Trips++;
            }

            if (shouldOpen)
            {
                State = CircuitState.Open;
                OpenedAt = now;
            }
        }
    }
}
