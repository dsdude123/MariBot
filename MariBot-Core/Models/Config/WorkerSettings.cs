using MariBot.Common.Model.GpuWorker;

namespace MariBot.Core.Models.Config
{
    /// <summary>
    /// The WorkerSettings section of appsettings.json, holding everything about
    /// how Core talks to its workers.
    /// </summary>
    /// <remarks>
    /// Every value has a working default except <see cref="PreSharedKey"/>,
    /// which has none on purpose: without a key configured, registration is
    /// refused outright rather than left open.
    /// </remarks>
    public class WorkerSettings
    {
        public const string SectionName = "WorkerSettings";

        /// <summary>
        /// Shared secret a worker must present to register or heartbeat. Unset
        /// means no worker can register at all.
        /// </summary>
        public string? PreSharedKey { get; set; }

        /// <summary>How often a registered worker must check in.</summary>
        public int HeartbeatSeconds { get; set; } = 15;

        /// <summary>
        /// Missed heartbeats before a worker is shown Offline. Above one so a
        /// single dropped request does not flicker the admin page.
        /// </summary>
        public int OfflineAfterMissedHeartbeats { get; set; } = 3;

        /// <summary>
        /// Missed heartbeats before a worker is removed entirely. This is what
        /// keeps a decommissioned machine from lingering in the list forever.
        /// </summary>
        public int EvictAfterMissedHeartbeats { get; set; } = 8;

        /// <summary>
        /// Applied to commands whose profile does not name a timeout of its own.
        /// </summary>
        public int DefaultJobTimeoutSeconds { get; set; } = 120;

        public int CircuitBreakerFailureThreshold { get; set; } = 3;

        public int CircuitBreakerCooldownSeconds { get; set; } = 60;

        /// <summary>How long finished-job metrics are kept before being pruned.</summary>
        public int MetricsRetentionDays { get; set; } = 30;

        public TimeSpan Heartbeat => TimeSpan.FromSeconds(Math.Max(1, HeartbeatSeconds));

        public TimeSpan OfflineAfter => Heartbeat * Math.Max(1, OfflineAfterMissedHeartbeats);

        public TimeSpan EvictAfter => Heartbeat * Math.Max(1, EvictAfterMissedHeartbeats);

        public TimeSpan DefaultJobTimeout => TimeSpan.FromSeconds(Math.Max(1, DefaultJobTimeoutSeconds));

        public CircuitBreakerOptions BreakerOptions => new()
        {
            FailureThreshold = Math.Max(1, CircuitBreakerFailureThreshold),
            Cooldown = TimeSpan.FromSeconds(Math.Max(1, CircuitBreakerCooldownSeconds))
        };

        /// <summary>
        /// How long <paramref name="command"/> may run: its own timeout where it
        /// declares one, otherwise the deployment default.
        /// </summary>
        public TimeSpan TimeoutFor(Command command) =>
            CommandRegistry.For(command).Timeout ?? DefaultJobTimeout;
    }
}
