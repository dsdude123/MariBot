using LiteDB;

namespace MariBot.Core.Models
{
    public class TwitterSubscription
    {
        public string Id { get; set; }
        public Dictionary<ulong, HashSet<ulong>> GuildChannelSubscriptions { get; set; }

        public HashSet<ulong> PostedIds { get; set; }
    }
}
