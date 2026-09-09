namespace MariBot.Common.Model.GpuWorker
{
    /// <summary>
    /// Tuning for a worker's <see cref="CircuitBreaker"/>.
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Consecutive failures that open the breaker. Above one, so that a
        /// single bad job — a malformed image, a worker restarting mid-request —
        /// does not take a healthy worker out of rotation.
        /// </summary>
        public int FailureThreshold { get; set; } = 3;

        /// <summary>
        /// How long an open breaker waits before letting one trial job through.
        /// </summary>
        public TimeSpan Cooldown { get; set; } = TimeSpan.FromMinutes(1);
    }
}
