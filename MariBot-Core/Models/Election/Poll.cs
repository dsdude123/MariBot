namespace MariBot.Core.Models.Election
{
    public class Poll
    {
        public string Id => GenerateId();
        public string PollKey { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        //public ulong OwnerId { get; set; }
        public string? Title {  get; set; }
        public string? Question { get; set; }
        public List<string> Canidates { get; set; }
        public DateTime CloseTime { get; set; }
        public PollOptions Config { get; set; }
        public PollStatus Status { get; set; }
        public IOrderedEnumerable<Result>? Results { get; set; }

        public string GenerateId()
        {
            return $"{PollKey.ToLower()}:{GuildId}:{ChannelId}";
        }
    }
}
