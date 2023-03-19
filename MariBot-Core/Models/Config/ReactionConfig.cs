namespace MariBot.Core.Models.Config
{
    public class ReactionConfig
    {
        /// <summary>
        /// List of words that activate the auto reaction.
        /// </summary>
        public string[] TriggerWords { get; set; }
        
        /// <summary>
        /// List of user IDs that the auto reaction may be applied on.
        /// </summary>
        public ulong[] TriggerUsers { get; set; }
        
        /// <summary>
        /// Discord emoji string to react with.
        /// </summary>
        public string Emoji { get; set; }
    }
}
