namespace MariBot.Core.Models.Sleeper
{
    /// <summary>
    /// Response entry from /players/nfl/trending/{add|drop}.
    /// </summary>
    public class TrendingPlayer
    {
        public string player_id { get; set; }

        /// <summary>Number of adds or drops across all Sleeper leagues in the lookback window.</summary>
        public int count { get; set; }
    }
}
