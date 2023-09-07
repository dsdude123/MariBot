using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class Pick
    {
        public string round { get; set; }

        [XmlElement(ElementName = "source_team_key")]
        public string sourceTeamKey { get; set; }

        [XmlElement(ElementName = "source_team_name")]
        public string sourceTeamName { get; set; }

        [XmlElement(ElementName = "destination_team_key")]
        public string destinationTeamKey { get; set; }

        [XmlElement(ElementName = "destination_team_name")]
        public string destinationTeamName { get; set; }

        [XmlElement(ElementName = "original_team_key")]
        public string originalTeamKey { get; set; }

        [XmlElement(ElementName = "original_team_name")]
        public string originalTeamName { get; set; }
    }
}
