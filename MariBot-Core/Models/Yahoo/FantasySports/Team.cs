using System.Security.Policy;
using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class Team
    {
        [XmlElement(ElementName = "team_key")]
        public string teamKey { get; set; }

        [XmlElement(ElementName = "team_id")]
        public string teamId { get; set; }
        public string name { get; set; }
        public string url { get; set; }

        [XmlElement("team_logos")]
        public TeamLogos teamLogos { get; set; }

        [XmlElement(ElementName = "division_id")]
        public string divisionId { get; set; }

        [XmlElement(ElementName = "faab_balance")]
        public string faabBalance { get; set; }

        [XmlElement(ElementName = "clinched_playoffs")]
        public string clinchedPlayoffs { get; set; }

        public Managers managers { get; set; }

        [XmlElement(ElementName = "team_points")]
        public TeamPoints teamPoints { get; set; }

        [XmlElement(ElementName = "team_projected_points")]
        public TeamPoints teamProjectedPoints { get; set; }

        [XmlElement("matchup")]
        public List<Matchup> matchups { get; set; }

        [XmlElement(ElementName = "team_standings")]
        public TeamStandings teamStandings { get; set; }
    }
}
