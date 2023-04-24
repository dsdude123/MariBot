namespace MariBot.Core.Models.Config
{
    public class GuildConfig
    {
        /// <summary>
        /// Guild ID
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Guild Name. For informational use only.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Array of features that are enabled for the guild.
        /// </summary>
        public string[] EnabledFeatures { get; set; }
        
        /// <summary>
        /// List of text commands that the guild is not allowed to run.
        /// </summary>
        public string[] BlockedTextCommands { get; set; }
        
        /// <summary>
        /// List of slash commands that the guild is not allowed to run.
        /// </summary>
        public string[] BlockedSlashCommands { get; set; }
        
        /// <summary>
        /// Automatic emoji reactions configured for the guild.
        /// </summary>
        public ReactionConfig[] AutoReactions { get; set; }
    }
}
