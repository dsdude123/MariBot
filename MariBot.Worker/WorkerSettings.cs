namespace MariBot.Worker
{
    /// <summary>
    /// The WorkerSettings section: everything this worker needs to find Core and
    /// tell it what it can do.
    /// </summary>
    public class WorkerSettings
    {
        public const string SectionName = "WorkerSettings";

        /// <summary>
        /// Base address of the Core instance to register with, e.g.
        /// http://maribot-core:8091. Empty disables registration entirely, for
        /// running a worker standalone against hand-posted jobs.
        /// </summary>
        public string? CoreEndpoint { get; set; }

        /// <summary>Must match Core's WorkerSettings:PreSharedKey.</summary>
        public string? PreSharedKey { get; set; }

        /// <summary>Display name in Core's admin page. Defaults to the hostname.</summary>
        public string? Name { get; set; }

        /// <summary>
        /// The address Core should post jobs to.
        /// </summary>
        /// <remarks>
        /// The worker has to state this itself: behind Docker networking, the
        /// only party that knows which name resolves to this container is this
        /// container. Defaults to http://{hostname}:{port}, which is right on a
        /// compose network and wrong behind NAT — set it explicitly there.
        /// </remarks>
        public string? AdvertisedEndpoint { get; set; }

        /// <summary>
        /// Capability names this worker claims, e.g. ["CPU"]. Claiming a GPU
        /// capability this worker cannot actually serve means those jobs are
        /// routed here and fail.
        /// </summary>
        public string[] Capabilities { get; set; } = { "CPU" };

        /// <summary>
        /// Fallback deadline for a job that arrives without one from Core.
        /// </summary>
        public int DefaultJobTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Seconds between registration attempts while Core is unreachable.
        /// </summary>
        public int RegistrationRetrySeconds { get; set; } = 15;

        public bool IsRegistrationConfigured =>
            !string.IsNullOrWhiteSpace(CoreEndpoint) && !string.IsNullOrWhiteSpace(PreSharedKey);
    }
}
