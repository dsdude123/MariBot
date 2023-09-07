using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class TeamLogo
    {
        public string size { get; set; }
        public string url { get; set; }
    }

    public class TeamLogos
    {
        [XmlElement("team_logo")]
        public List<TeamLogo> teamLogo { get; set; }
    }
}
