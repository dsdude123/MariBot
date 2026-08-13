namespace MariBot.Core.Models.Sleeper
{
    /// <summary>
    /// Response entry from /league/{league_id}/users.
    /// </summary>
    public class LeagueUser
    {
        public string user_id { get; set; }
        public string username { get; set; }
        public string display_name { get; set; }
        public string avatar { get; set; }
        public bool? is_owner { get; set; }
        public UserMetadata metadata { get; set; }

        public class UserMetadata
        {
            public string team_name { get; set; }
        }
    }
}
