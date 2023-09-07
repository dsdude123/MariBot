using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    public class TeamStandings
    {
        public uint rank { get; set; }

        [XmlElement(ElementName = "outcome_totals")]
        public OutcomeTotals outcomeTotals { get; set; }

        [XmlElement(ElementName = "divisional_outcome_totals")]
        public OutcomeTotals divisionalOutcomeTotals { get; set; }
    }
}
