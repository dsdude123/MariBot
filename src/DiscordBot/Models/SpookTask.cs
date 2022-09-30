using LiteDB;

namespace MariBot.Models
{
    public class SpookTask
    {
        [BsonId]
        public ulong UserId { get; set; }

        [BsonField]
        public ulong SpookedByUserId { get; set; }

        [BsonField]
        public bool TaskComplete { get; set; }
    }
}
