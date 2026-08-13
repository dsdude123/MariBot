namespace MariBot.Core.Models.Sleeper
{
    /// <summary>
    /// A trimmed entry from the /players/nfl catalog. The full payload is ~14 MB
    /// across ~12,000 players with dozens of fields each; only the fields needed to
    /// render an embed are kept, which is what makes the cached copy affordable to
    /// hold in memory and to write back to disk.
    /// </summary>
    public class SleeperPlayer
    {
        public string player_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }

        /// <summary>Null for team defense entries, which are keyed by team abbreviation.</summary>
        public string full_name { get; set; }

        public string position { get; set; }
        public string team { get; set; }
        public string injury_status { get; set; }
        public int? number { get; set; }
        public List<string> fantasy_positions { get; set; }

        /// <summary>
        /// Best available display name. Defense entries have no full_name but do have
        /// first/last ("Cleveland" / "Browns"), so fall through rather than showing blank.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(full_name))
                    return full_name;

                var joined = $"{first_name} {last_name}".Trim();
                return string.IsNullOrWhiteSpace(joined) ? player_id : joined;
            }
        }
    }
}
