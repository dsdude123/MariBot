using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class Manager
    {
        [XmlElement(ElementName = "manager_id")]
        public string managerId { get; set; }

        public string nickname { get; set; }
        public string guid { get; set; }
    }

    [XmlRoot("managers")]
    public class Managers
    {
        [XmlElement("manager")]
        public List<Manager> managers { get; set; }
    }
}
