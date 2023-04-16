using LiteDB;

namespace MariBot.Core.Models
{
    public class TwitterSubscription
    {
        [BsonId]
        public string Username { get; set; }
        public Dictionary<ulong, HashSet<ulong>> GuildChannelSubscriptions { get; set; }

        public HashSet<ulong> PostedIds { get; set; }
    }
}
