using Grpc.Core;
using Grpc.Net.Client;
using XaiApi;

namespace MariBot.Core.Services;

public class GrokService
{
    private readonly GrpcChannel grpcChannel;
    private readonly Metadata headers;
    private readonly DynamicConfigService dynamicConfigService;
    private readonly ILogger<GrokService> logger;

    protected GrokService() { }

    public GrokService(IConfiguration configuration, DynamicConfigService dynamicConfigService, ILogger<GrokService> logger)
    {
        grpcChannel = GrpcChannel.ForAddress("https://api.x.ai");
        headers = new Metadata
        {
            { "Authorization", $"Bearer {configuration["DiscordSettings:GrokApiKey"]}" }
        };
        this.dynamicConfigService = dynamicConfigService;
        this.logger = logger;
    }

    /// <summary>
    /// Generate Grok response for a given prompt
    /// </summary>
    /// <param name="prompt">Prompt</param>
    /// <returns>Response from Grok</returns>
    public virtual async Task<string> GetGrokResponseAsync(string prompt)
    {
        var chatClient = new Chat.ChatClient(grpcChannel);

        var request = new GetCompletionsRequest
        {
            Model = dynamicConfigService.GetGrokChatModel(),
            Messages =
            {
                new Message
                {
                    Content =
                    {
                        new Content
                        {
                            Text = prompt
                        }
                    },
                    Role = MessageRole.RoleUser
                }
            },
            ToolChoice = new ToolChoice
            {
                Mode = ToolMode.Auto
            },
            Tools =
            {
                new Tool
                {
                    WebSearch = new WebSearch()
                },
                new Tool
                {
                    XSearch = new XSearch()
                }
            }
        };

        var response = await chatClient.GetCompletionAsync(request, headers);

        var text = response.Outputs
            .Select(o => o.Message?.Content)
            .FirstOrDefault(c => !string.IsNullOrEmpty(c));

        return text ?? "Unknown or no response from Grok.";
    }

    /// <summary>
    /// Generates an image using Grok's image generation tool
    /// </summary>
    /// <param name="prompt">Prompt</param>
    /// <returns>URI</returns>
    public virtual async Task<string> GetGrokImageAsync(string prompt)
    {
        var imageClient = new Image.ImageClient(grpcChannel);

        var request = new GenerateImageRequest
        {
            Model = dynamicConfigService.GetGrokImageModel(),
            Prompt = prompt,
            N = 1,
            Format = ImageFormat.ImgFormatUrl
        };

        var response = await imageClient.GenerateImageAsync(request, headers);
        return response.Images.First().Url;
    }
}