namespace MariBot.Common.Model.GpuWorker
{
    /// <summary>
    /// How a job ended. Distinguishes failures that implicate the worker from
    /// ones that do not, which is what the circuit breaker keys off.
    /// </summary>
    public enum JobOutcome
    {
        /// <summary>The worker returned a result. Says nothing about whether the user liked it.</summary>
        Completed,

        /// <summary>The worker refused the job or was unreachable when we tried to hand it over.</summary>
        DispatchFailed,

        /// <summary>The worker took the job and never came back inside its deadline.</summary>
        TimedOut,

        /// <summary>
        /// Never dispatched — every worker that could have run it went out of
        /// service while it waited. Not any single worker's fault.
        /// </summary>
        Dropped
    }
}
