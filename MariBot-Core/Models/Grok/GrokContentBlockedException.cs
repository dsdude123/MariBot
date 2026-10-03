namespace MariBot.Core.Models.Grok
{
    /// <summary>
    /// Thrown when Grok refuses a request because the prompt or the generated content
    /// did not pass xAI's content moderation rules.
    /// </summary>
    public class GrokContentBlockedException : Exception
    {
        public GrokContentBlockedException(string message) : base(message)
        {
        }

        public GrokContentBlockedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
