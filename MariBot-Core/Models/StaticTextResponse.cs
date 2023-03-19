namespace MariBot.Core.Models
{
    public class StaticTextResponse
    {
        public string Id => GenerateId();
        public string Command { get; set; }
        public ulong GuildId { get; set; }
        public bool IsGlobal { get; set; }
        public string Message { get; set; }

        private string GenerateId()
        {
            if (IsGlobal)
            {
                return $"{Command.ToLower()}:global";
            }
            else
            {
                return $"{Command.ToLower()}:{GuildId}";
            }
        }
    }
}
