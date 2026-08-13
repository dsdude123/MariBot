namespace MariBot.Core.Models.Sleeper
{
    /// <summary>
    /// Response entry from /league/{league_id}/transactions/{round}.
    /// </summary>
    public class Transaction
    {
        public string type { get; set; }
        public string transaction_id { get; set; }
        public string status { get; set; }

        /// <summary>Epoch milliseconds.</summary>
        public long status_updated { get; set; }

        /// <summary>Epoch milliseconds.</summary>
        public long created { get; set; }

        public int leg { get; set; }

        /// <summary>Every roster taking part. Trades can involve three or more.</summary>
        public List<int> roster_ids { get; set; }

        public List<int> consenter_ids { get; set; }

        /// <summary>
        /// player_id to the roster_id receiving them. Null when nothing was added,
        /// so the destination team comes from the value here rather than from
        /// <see cref="roster_ids"/>, which does not say who got what.
        /// </summary>
        public Dictionary<string, int> adds { get; set; }

        /// <summary>player_id to the roster_id giving them up. Null when nothing was dropped.</summary>
        public Dictionary<string, int> drops { get; set; }

        public List<DraftPick> draft_picks { get; set; }

        /// <summary>Null for trades — Sleeper only populates this for waiver claims.</summary>
        public TransactionSettings settings { get; set; }

        public class TransactionSettings
        {
            public int? waiver_bid { get; set; }
            public int? seq { get; set; }
        }
    }
}
