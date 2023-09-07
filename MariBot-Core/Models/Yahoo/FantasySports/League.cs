using System.Xml.Serialization;

namespace MariBot.Core.Models.Yahoo.FantasySports
{
    // TODO: Strongly type these elements
    public class League
    {
        [XmlElement(ElementName = "league_key")]
        public string leagueKey { get; set; }

        [XmlElement(ElementName = "league_id")]
        public string leagueId { get; set; }

        [XmlElement(ElementName = "logo_url")]
        public string logoUrl { get; set; }

        public string name { get; set; }
        public string url { get; set; }

        [XmlElement(ElementName = "draft_status")]
        public string draftStatus { get; set; }

        [XmlElement(ElementName = "num_teams")]
        public string numTeams { get; set; }

        [XmlElement(ElementName = "weekly_deadline")]
        public string weeklyDeadline { get; set; }

        [XmlElement(ElementName = "league_update_timestamp")]
        public string leagueUpdateTimestamp { get; set; }

        [XmlElement(ElementName = "scoring_type")]
        public string scoringType { get; set; }

        [XmlElement(ElementName = "current_week")]
        public string currentWeek { get; set; }

        [XmlElement(ElementName = "start_week")]
        public string startWeek { get; set; }

        [XmlElement(ElementName = "end_week")]
        public string endWeek { get; set; }

        [XmlElement(ElementName = "is_finished")]
        public bool isFinished { get; set; }
        public Scoreboard scoreboard { get; set; }

        public Standings standings { get; set; }

        [XmlElement(ElementName = "transaction")]
        public List<Transaction> transactions { get; set; }
    }
}
