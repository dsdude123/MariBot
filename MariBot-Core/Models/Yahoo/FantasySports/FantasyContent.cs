using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    [XmlRoot("fantasy_content")]
    public class FantasyContent
    {
        public League league { get; set; }
    }
}
