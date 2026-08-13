namespace MariBot.Core.Models.Sleeper
{
    /// <summary>
    /// A traded draft pick, as it appears in a transaction's draft_picks array.
    /// </summary>
    public class DraftPick
    {
        public string season { get; set; }
        public int round { get; set; }

        /// <summary>The roster the pick originally belonged to.</summary>
        public int roster_id { get; set; }

        public int previous_owner_id { get; set; }

        /// <summary>The roster receiving the pick in this transaction.</summary>
        public int owner_id { get; set; }
    }
}
