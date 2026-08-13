namespace MariBot.Core.Models
{
    /// <summary>
    /// A guild's Sleeper league binding, created at runtime by
    /// <c>z fantasy subscribe</c>. This replaces the compiled-in guild-to-league and
    /// guild-to-channel dictionaries the Yahoo integration used, so moving a league
    /// no longer needs a rebuild.
    /// </summary>
    public class SleeperSubscription
    {
        /// <summary>
        /// The guild id, so a command can look the subscription up directly. One
        /// league per guild — the guild that uses this runs a single league.
        /// </summary>
        public string Id => GuildId.ToString();

        public ulong GuildId { get; set; }

        public string LeagueId { get; set; }

        /// <summary>Where the poller announces transactions — the channel subscribe was run in.</summary>
        public ulong AnnouncementChannelId { get; set; }

        /// <summary>
        /// Transaction ids already announced. This is the dedupe: a persisted set
        /// rather than a time window, so a restart cannot re-announce. Trimmed to the
        /// current and previous round on each poll so it does not grow across a season.
        /// </summary>
        public HashSet<string> PostedTransactionIds { get; set; } = new();

        /// <summary>
        /// Epoch milliseconds of the newest transaction seen. Used to keep the first
        /// poll after subscribing from dumping the whole week into the channel.
        /// </summary>
        public long LastTransactionTimestamp { get; set; }
    }
}
