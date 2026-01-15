using GrokSdk;
using GrokSdk.Tools;

namespace MariBot.Core.Services;

public class GrokService
{
    private readonly GrokClient grokClient;
    private readonly ILogger<GrokService> logger;

    public GrokService(IConfiguration configuration, ILogger<GrokService> logger)
    {
        grokClient = new GrokClient(new HttpClient(), configuration["DiscordSettings:GrokApiKey"]);
        this.logger = logger;
    }

    /// <summary>
    /// Generate Grok response for a given prompt
    /// </summary>
    /// <param name="prompt">Prompt</param>
    /// <returns>Response from Grok</returns>
    public async Task<string> GetGrokResponseAsync(string prompt)
    {
        try
        {
            var thread = grokClient.GetGrokThread();
            thread.RegisterTool(new GrokToolReasoning(grokClient));
            thread.RegisterTool(new GrokToolLiveSearch(grokClient));
            var fullResult = "";
            await foreach (var message in thread.AskQuestion(prompt))
            {
                fullResult += message + "\n";
            }

            return fullResult.Trim();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error while getting Grok response");
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Generates an image using Grok's image generation tool
    /// </summary>
    /// <param name="prompt">Prompt</param>
    /// <returns>URI</returns>
    public async Task<string> GetGrokImageAsync(string prompt)
    {
        try
        {
            var thread = grokClient.GetGrokThread();
            thread.RegisterTool(new GrokToolImageGeneration(grokClient));
            var fullResult = "";
            await foreach (var message in thread.AskQuestion(prompt))
            {
                fullResult += message + "\n";
            }

            return fullResult.Trim();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error while generating Grok image");
            return $"Error: {ex.Message}";
        }
    }
}