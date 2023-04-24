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
    }
}
