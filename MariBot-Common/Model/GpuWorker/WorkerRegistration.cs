namespace MariBot.Common.Model.GpuWorker
{
    /// <summary>
    /// What a worker sends Core to announce itself.
    /// </summary>
    /// <remarks>
    /// Capabilities travel as strings rather than the enum so the endpoint can
    /// be driven by hand with curl and so a worker naming a capability this Core
    /// does not know is a clear rejection rather than a silent zero.
    /// </remarks>
    public class WorkerRegistration
    {
        /// <summary>Display name; the worker's hostname unless it was configured otherwise.</summary>
        public string? Name { get; set; }

        /// <summary>
        /// Base address Core should post jobs to. The worker has to state this
        /// itself — behind Docker networking it is the only party that knows
        /// which name resolves to it.
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>Capability names, e.g. ["CPU", "ConsumerGPU"].</summary>
        public string[]? Capabilities { get; set; }
    }

    /// <summary>
    /// Core's answer to a registration: who the worker now is, and how often it
    /// has to check in to stay registered.
    /// </summary>
    public class WorkerRegistrationResult
    {
        public Guid WorkerId { get; set; }

        /// <summary>Seconds between heartbeats the worker should send.</summary>
        public int HeartbeatSeconds { get; set; }

        /// <summary>
        /// Seconds of silence after which Core drops the worker. The worker does
        /// not need it, but it makes the contract visible in the response.
        /// </summary>
        public int EvictAfterSeconds { get; set; }
    }
}
