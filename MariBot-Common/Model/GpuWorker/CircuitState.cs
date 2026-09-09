namespace MariBot.Common.Model.GpuWorker
{
    /// <summary>
    /// State of a worker's circuit breaker.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="WorkerStatus"/>, which says what the worker is
    /// doing; this says whether we are still willing to send it work. A worker
    /// can be Ready and Open at the same time — up and answering, but having
    /// failed enough jobs recently that we are holding off.
    /// </remarks>
    public enum CircuitState
    {
        /// <summary>Normal operation. Jobs dispatch.</summary>
        Closed,

        /// <summary>Failing. No jobs dispatch until the cooldown elapses.</summary>
        Open,

        /// <summary>
        /// Cooldown elapsed. Exactly one job is allowed through to find out
        /// whether the worker recovered.
        /// </summary>
        HalfOpen
    }
}
