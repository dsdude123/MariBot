namespace MariBot.Common.Model.GpuWorker
{
    /// <summary>
    /// Everything the dispatcher needs to know about one <see cref="Command"/>:
    /// what kind of worker can run it, which queue it waits in, and how long it
    /// is allowed to take.
    /// </summary>
    /// <param name="Capability">Worker capability required to run the command.</param>
    /// <param name="Priority">Queue the command waits in.</param>
    /// <param name="Timeout">
    /// How long the command may run before it is abandoned, or null to use the
    /// deployment-wide default. Set it only where a command is genuinely slower
    /// than the rest — image generation, not a face detect.
    /// </param>
    public record CommandProfile(
        WorkerCapability Capability,
        JobPriority Priority = JobPriority.Normal,
        TimeSpan? Timeout = null);
}
