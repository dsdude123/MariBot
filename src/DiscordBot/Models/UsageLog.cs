using LiteDB;
using System;

namespace MariBot.Models
{
    public class UsageLog
    {
        [BsonId]
        public int Id { get; set; }

        [BsonField]
        public ActionType ActionType { get; set; }

        [BsonField]
        public ulong UserId { get; set; }

        [BsonField]
        public DateTime Timestamp { get; set; }
    }
}
