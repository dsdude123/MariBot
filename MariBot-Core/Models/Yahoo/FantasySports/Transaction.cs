using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class Transaction
    {
        [XmlElement(ElementName = "transaction_key")]
        public string transactionKey { get; set; }

        [XmlElement(ElementName = "transaction_id")]
        public string transactionId { get; set; }

        public string type { get; set; }
        public string status { get; set; }
        public string timestamp { get; set; }

        [XmlElement(ElementName = "waiver_player_key")]
        public string waiverPlayerKey { get; set; }

        [XmlElement(ElementName = "waiver_team_key")]
        public string waiverTeamKey { get; set; }

        [XmlElement(ElementName = "waiver_date")]
        public string waiverDate { get; set; }

        [XmlElement(ElementName = "waiver_priority")]
        public string waiverPriority { get; set; }

        [XmlElement(ElementName = "trader_team_key")]
        public string traderTeamKey { get; set; }

        [XmlElement(ElementName = "trader_team_name")]
        public string traderTeamName { get; set; }

        [XmlElement(ElementName = "tradee_team_key")]
        public string tradeeTeamKey { get; set; }

        [XmlElement(ElementName = "tradee_team_name")]
        public string tradeeTeamName { get; set; }

        [XmlElement(ElementName = "trade_proposed_time")]
        public string tradeProposedTime { get; set; }

        [XmlElement(ElementName = "trade_note")]
        public string tradeNote { get; set; }

        [XmlElement("player")]
        public List<Player> players { get; set; }

        [XmlElement("pick")]
        public List<Pick> picks { get; set; }
    }
}
