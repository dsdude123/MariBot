namespace MariBot.Core.Models.ChatGPT
{
    public class MessageHistory
    {
        public MessageHistory()
        {
            Messages = new List<Tuple<MessageType, string>>();
        }

        public string Id => $"{GuildId}-{ChannelId}-{MessageId}";
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public List<Tuple<MessageType, string>> Messages { get; set; }
    }
}
