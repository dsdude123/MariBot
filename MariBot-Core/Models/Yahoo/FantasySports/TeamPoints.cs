using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class TeamPoints
    {
        [XmlElement(ElementName = "coverage_type")]
        public string coverageType { get; set; }

        public string season { get; set; }

        public double total { get; set; }
    }
}
