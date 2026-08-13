namespace MariBot.Core.Models.Sleeper
{
    /// <summary>
    /// Response from /state/nfl. Property names match the Sleeper wire format verbatim.
    /// </summary>
    public class NflState
    {
        public int week { get; set; }
        public int display_week { get; set; }

        /// <summary>
        /// The scoring period used as the "round" for transaction lookups. This is
        /// 0 during the preseason, so callers must clamp it before using it in a URL.
        /// </summary>
        public int leg { get; set; }

        public string season { get; set; }
        public string season_type { get; set; }
        public string previous_season { get; set; }
    }
}
