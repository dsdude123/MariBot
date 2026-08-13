namespace MariBot.Core.Models.Sleeper
{
    /// <summary>
    /// Response entry from /league/{league_id}/matchups/{week}. Sleeper returns one
    /// entry per roster and does not pair them — entries sharing a matchup_id are
    /// opponents, and a null matchup_id means the roster has no opponent that week.
    /// </summary>
    public class Matchup
    {
        public int roster_id { get; set; }
        public int? matchup_id { get; set; }
        public double points { get; set; }
        public double? custom_points { get; set; }
        public List<string> starters { get; set; }
        public List<string> players { get; set; }
    }
}
