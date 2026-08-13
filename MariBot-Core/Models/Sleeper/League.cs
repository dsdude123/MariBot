namespace MariBot.Core.Models.Sleeper
{
    /// <summary>
    /// Response from /league/{league_id}.
    /// </summary>
    public class League
    {
        public string league_id { get; set; }
        public string name { get; set; }
        public string status { get; set; }
        public string season { get; set; }
        public string season_type { get; set; }
        public int total_rosters { get; set; }
        public string avatar { get; set; }
        public string draft_id { get; set; }
        public string previous_league_id { get; set; }

        /// <summary>
        /// League settings are a loose bag of ints that varies by league format,
        /// so it stays untyped rather than pretending to a fixed shape.
        /// </summary>
        public Dictionary<string, object> settings { get; set; }

        public Dictionary<string, object> scoring_settings { get; set; }
        public List<string> roster_positions { get; set; }
    }
}
