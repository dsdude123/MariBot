using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class Matchup
    {
        public uint week { get; set; }
        public string status { get; set; }

        [XmlElement("is_tied")]
        public bool isTied { get; set; }

        [XmlElement("winner_team_key")]
        public string winnerTeamKey { get; set; }

        [XmlElement("team")]
        public List<Team> teams { get; set; }
    }
}
