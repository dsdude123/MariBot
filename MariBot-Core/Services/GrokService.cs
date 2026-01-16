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
            var rawResponse = "";
            await foreach (var message in thread.AskQuestion(prompt))
            {
                rawResponse += message.ToString();
                if (message is GrokTextMessage textMessage)
                {
                    return textMessage.Message;
                }
                if (message is GrokToolResponse response && (response.ToolName == GrokToolLiveSearch.ToolName 
                                                             || response.ToolName == GrokToolReasoning.ToolName))
                {
                    var jsonResponse = response.ToolResponse;
                    string summary = null;
                    using (var doc = System.Text.Json.JsonDocument.Parse(jsonResponse))
                    {
                        if (doc.RootElement.TryGetProperty("summary", out var el) 
                            && el.ValueKind == System.Text.Json.JsonValueKind.String)
                            summary = el.GetString();
                    }
                    if (!string.IsNullOrEmpty(summary))
                    {
                        return summary;
                    }

                    return jsonResponse;
                }
            }
            return $"Unknown or no response from Grok. {rawResponse}";
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
        GrokImageGenerationRequest request = new GrokImageGenerationRequest()
        {
            Prompt = prompt,
            N = 1,
            Response_format = GrokImageGenerationRequestResponse_format.Url
        };
        var generatedImage = await grokClient.GenerateImagesAsync(request);
        return generatedImage.Data.First().Url;
    }
}