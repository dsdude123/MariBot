using LiteDB;
using System;

namespace MariBot.Models
{
    public class SpookedUser
    {
        [BsonId]
        public ulong UserId { get; set; }

        [BsonField]
        public string SpookedNickname { get; set; }

        [BsonField]
        public DateTime SpookedTime { get; set; }

        [BsonField]
        public ulong? SpookedByUserId { get; set; }

        [BsonField]
        public string OriginalNickname { get; set; }
    }
}
