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

    /// <summary>
    /// Edits an image (or combines two images) using Grok's image editing tool
    /// </summary>
    /// <param name="prompt">Editing instructions</param>
    /// <param name="imageUrls">1-2 image URLs to edit</param>
    /// <returns>URL of the edited image</returns>
    public virtual async Task<string> GetGrokImageEditAsync(string prompt, List<string> imageUrls)
    {
        var imageClient = new Image.ImageClient(grpcChannel);

        var request = new GenerateImageRequest
        {
            Model = dynamicConfigService.GetGrokImageModel(),
            Prompt = prompt,
            N = 1,
            Format = ImageFormat.ImgFormatUrl
        };

        if (imageUrls.Count == 1)
        {
            request.Image = new ImageUrlContent { ImageUrl = imageUrls[0] };
        }
        else
        {
            foreach (var url in imageUrls)
            {
                request.Images.Add(new ImageUrlContent { ImageUrl = url });
            }
        }

        var response = await imageClient.GenerateImageAsync(request, headers);
        return response.Images.First().Url;
    }

    /// <summary>
    /// Generates a video using Grok's video generation tool
    /// </summary>
    /// <param name="prompt">Prompt</param>
    /// <param name="duration">Duration in seconds</param>
    /// <returns>Video URL</returns>
    public virtual async Task<string> GetGrokVideoAsync(string prompt, int duration)
    {
        var videoClient = new Video.VideoClient(grpcChannel);

        var request = new GenerateVideoRequest
        {
            Model = dynamicConfigService.GetGrokVideoModel(),
            Prompt = prompt,
            Duration = duration
        };

        var generateResponse = await videoClient.GenerateVideoAsync(request, headers);
        var requestId = generateResponse.RequestId;

        while (true)
        {
            await Task.Delay(5000);

            var deferredResponse = await videoClient.GetDeferredVideoAsync(
                new GetDeferredVideoRequest { RequestId = requestId }, headers);

            if (deferredResponse.Status == DeferredStatus.Done)
            {
                return deferredResponse.Response.Video.Url;
            }
        }
    }
}