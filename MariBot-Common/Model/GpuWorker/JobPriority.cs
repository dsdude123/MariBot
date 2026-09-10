namespace MariBot.Common.Model.GpuWorker
{
    /// <summary>
    /// Which queue a job waits in. Strictly ordered: every waiting
    /// <see cref="High"/> job is dispatched before any <see cref="Normal"/> one.
    /// </summary>
    public enum JobPriority
    {
        /// <summary>
        /// The default. Anything whose runtime is measured in seconds and whose
        /// caller is not watching a half-finished message.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Near-instant work that a slow job must not sit in front of — a radar
        /// frame or a format conversion finishing after a deepfry would be a
        /// minute of dead air for something that took 200ms to compute.
        /// </summary>
        High = 1
    }
}
