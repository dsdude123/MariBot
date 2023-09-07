using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class Player
    {
        [XmlElement(ElementName = "player_key")]
        public string playerKey { get; set; }

        [XmlElement(ElementName = "player_id")]
        public string playerId { get; set; }

        public Name name { get; set; }

        [XmlElement(ElementName = "editorial_team_abbr")]
        public string editorialTeamAbbr { get; set; }

        [XmlElement(ElementName = "display_position")]
        public string displayPosition { get; set; }

        [XmlElement(ElementName = "transaction_data")]
        public TransactionData transactionData { get; set; }
    }
}
