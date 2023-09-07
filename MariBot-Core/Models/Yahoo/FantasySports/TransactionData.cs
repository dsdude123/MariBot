using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class TransactionData
    {
        public string type { get; set; }

        [XmlElement(ElementName = "source_type")]
        public string sourceType { get; set; }

        [XmlElement(ElementName = "source_team_key")]
        public string sourceTeamKey { get; set; }

        [XmlElement(ElementName = "source_team_name")]
        public string sourceTeamName { get; set; }

        [XmlElement(ElementName = "destination_type")]
        public string destinationType { get; set; }

        [XmlElement(ElementName = "destination_team_key")]
        public string destinationTeamKey { get; set; }

        [XmlElement(ElementName = "destination_team_name")]
        public string destinationTeamName { get; set; }
    }
}
