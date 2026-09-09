namespace MariBot.Core.Models
{
    /// <summary>
    /// The OpenAI models MariBot can dispatch a prompt to.
    /// </summary>
    /// <remarks>
    /// One member per model that is actually reachable, which is why there is no
    /// GPT3 or DALLE here any more: OpenAI shut dall-e-3 down in May 2026 and
    /// shuts gpt-3.5-turbo down in October 2026. Their commands still exist and
    /// still work — they warn and run on the model that replaced them — so the
    /// retirement lives in the command, not in a member pointing at something
    /// that no longer answers.
    /// </remarks>
    public enum OpenAiModel
    {
        GPT4,
        GPT5,
        GPTIMAGE
    }
}
