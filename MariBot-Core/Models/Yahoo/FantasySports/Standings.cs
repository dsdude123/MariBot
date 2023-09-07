using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class Standings
    {
        [XmlElement("team")]
        public List<Team> teams { get; set; }
    }
}
