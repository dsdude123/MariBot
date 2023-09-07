using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class Name
    {
        public string full { get; set; }
        public string first { get; set; }
        public string last { get; set; }

        [XmlElement(ElementName = "ascii_first")]
        public string asciiFirst { get; set; }

        [XmlElement(ElementName = "ascii_last")]
        public string asciiLast { get; set; }
    }
}
