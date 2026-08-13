namespace MariBot.Core.Models.Sleeper
{
    /// <summary>
    /// Response entry from /league/{league_id}/winners_bracket and /losers_bracket.
    /// </summary>
    public class BracketMatchup
    {
        /// <summary>Round number.</summary>
        public int r { get; set; }

        /// <summary>Match id, unique within the bracket. Referenced by <see cref="BracketSource"/>.</summary>
        public int m { get; set; }

        /// <summary>Roster id on either side, or null while the slot is still being decided.</summary>
        public int? t1 { get; set; }

        public int? t2 { get; set; }

        /// <summary>Winning roster id, null until the match is played.</summary>
        public int? w { get; set; }

        public int? l { get; set; }

        /// <summary>Set when t1 is not yet known: points at the match it feeds from.</summary>
        public BracketSource t1_from { get; set; }

        public BracketSource t2_from { get; set; }

        /// <summary>
        /// Where an undecided bracket slot comes from — the winner (w) or loser (l)
        /// of the referenced match id.
        /// </summary>
        public class BracketSource
        {
            public int? w { get; set; }
            public int? l { get; set; }
        }
    }
}
