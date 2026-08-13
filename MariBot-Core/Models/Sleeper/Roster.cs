namespace MariBot.Core.Models.Sleeper
{
    /// <summary>
    /// Response entry from /league/{league_id}/rosters.
    /// </summary>
    public class Roster
    {
        public int roster_id { get; set; }
        public string owner_id { get; set; }
        public string league_id { get; set; }
        public List<string> players { get; set; }
        public List<string> starters { get; set; }
        public List<string> reserve { get; set; }
        public RosterSettings settings { get; set; }

        public class RosterSettings
        {
            public int wins { get; set; }
            public int losses { get; set; }
            public int ties { get; set; }

            /// <summary>
            /// Sleeper splits fantasy points into a whole part and a hundredths part;
            /// 1776 + 6 means 1776.06, not 1776.6. Use <see cref="PointsFor"/>.
            /// </summary>
            public int fpts { get; set; }

            public int fpts_decimal { get; set; }
            public int fpts_against { get; set; }
            public int fpts_against_decimal { get; set; }
            public int waiver_position { get; set; }
            public int waiver_budget_used { get; set; }

            public double PointsFor => fpts + (fpts_decimal / 100.0);
            public double PointsAgainst => fpts_against + (fpts_against_decimal / 100.0);
        }
    }
}
