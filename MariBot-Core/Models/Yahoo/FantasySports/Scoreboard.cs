using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class Scoreboard
    {
        public uint week { get; set; }

        [XmlElement("matchup")]
        public List<Matchup> matchups { get; set; }
    }
}
