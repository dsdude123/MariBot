namespace MariBot.Core.Models.Config
{
    /// <summary>
    /// Base model for dynamic configuration.
    /// </summary>
    public class DynamicConfig
    {
        /// <summary>
        /// Guild specific configurations.
        /// </summary>
        public GuildConfig[] Guilds { get; set; }

        /// <summary>
        /// Model to use for Grok chat completions.
        /// </summary>
        public string GrokChatModel { get; set; } = "grok-4-1-fast-reasoning";

        /// <summary>
        /// Model to use for Grok image generation.
        /// </summary>
        public string GrokImageModel { get; set; } = "grok-imagine-image";
    }
}
