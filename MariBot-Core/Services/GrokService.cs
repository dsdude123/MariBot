using Grpc.Core;
using Grpc.Net.Client;
using MariBot.Core.Models.Grok;
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

        var response = await CallGrokAsync(() => chatClient.GetCompletionAsync(request, headers).ResponseAsync,
            "text generation");

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
    /// <exception cref="GrokContentBlockedException">Grok blocked the request or the generated image</exception>
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

        var response = await CallGrokAsync(() => imageClient.GenerateImageAsync(request, headers).ResponseAsync,
            "image generation");

        return ResolveImageUrl(response, "image generation");
    }

    /// <summary>
    /// Edits an image (or combines two images) using Grok's image editing tool
    /// </summary>
    /// <param name="prompt">Editing instructions</param>
    /// <param name="imageUrls">1-2 image URLs to edit</param>
    /// <returns>URL of the edited image</returns>
    /// <exception cref="GrokContentBlockedException">Grok blocked the request or the edited image</exception>
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

        var response = await CallGrokAsync(() => imageClient.GenerateImageAsync(request, headers).ResponseAsync,
            "image edit");

        return ResolveImageUrl(response, "image edit");
    }

    /// <summary>
    /// Generates a video using Grok's video generation tool
    /// </summary>
    /// <param name="prompt">Prompt</param>
    /// <param name="duration">Duration in seconds</param>
    /// <returns>Video URL</returns>
    /// <exception cref="GrokContentBlockedException">Grok blocked the request or the generated video</exception>
    public virtual async Task<string> GetGrokVideoAsync(string prompt, int duration)
    {
        var videoClient = new Video.VideoClient(grpcChannel);

        var request = new GenerateVideoRequest
        {
            Model = dynamicConfigService.GetGrokVideoModel(),
            Prompt = prompt,
            Duration = duration
        };

        var generateResponse = await CallGrokAsync(
            () => videoClient.GenerateVideoAsync(request, headers).ResponseAsync, "video generation");
        var requestId = generateResponse.RequestId;

        while (true)
        {
            await Task.Delay(5000);

            var deferredResponse = await CallGrokAsync(
                () => videoClient.GetDeferredVideoAsync(new GetDeferredVideoRequest { RequestId = requestId }, headers)
                    .ResponseAsync, "video generation");

            switch (deferredResponse.Status)
            {
                case DeferredStatus.Pending:
                    continue;
                case DeferredStatus.Done:
                    return ResolveVideoUrl(deferredResponse.Response, "video generation");
                case DeferredStatus.Expired:
                    throw new ApplicationException(
                        "Grok video generation finished but the result expired before it could be downloaded.");
                default:
                    throw new ApplicationException(
                        $"Grok video generation returned an unexpected status: {deferredResponse.Status}.");
            }
        }
    }

    /// <summary>
    /// Pulls the URL out of an image response, failing with a meaningful message when Grok
    /// returned a moderated placeholder or no usable image at all.
    /// </summary>
    /// <param name="response">Response from the image endpoint</param>
    /// <param name="operation">Operation name used in error messages</param>
    /// <returns>URL of the generated image</returns>
    /// <exception cref="GrokContentBlockedException">The image did not pass content moderation</exception>
    internal static string ResolveImageUrl(ImageResponse response, string operation)
    {
        var image = response.Images.FirstOrDefault();

        if (image == null)
        {
            throw new ApplicationException($"Grok {operation} did not return an image.");
        }

        // When content moderation rejects the result, Grok replaces the image with a
        // placeholder and clears the URL instead of returning an error.
        if (!image.RespectModeration)
        {
            throw new GrokContentBlockedException(
                $"Grok blocked this {operation} because the result did not pass its content moderation rules.");
        }

        if (string.IsNullOrWhiteSpace(image.Url))
        {
            throw new ApplicationException($"Grok {operation} did not return an image URL.");
        }

        return image.Url;
    }

    /// <summary>
    /// Pulls the URL out of a video response, failing with a meaningful message when Grok
    /// returned a moderated result or no usable video at all.
    /// </summary>
    /// <param name="response">Response from the video endpoint</param>
    /// <param name="operation">Operation name used in error messages</param>
    /// <returns>URL of the generated video</returns>
    /// <exception cref="GrokContentBlockedException">The video did not pass content moderation</exception>
    internal static string ResolveVideoUrl(VideoResponse? response, string operation)
    {
        var video = response?.Video;

        if (video == null)
        {
            throw new ApplicationException($"Grok {operation} did not return a video.");
        }

        if (!video.RespectModeration)
        {
            throw new GrokContentBlockedException(
                $"Grok blocked this {operation} because the result did not pass its content moderation rules.");
        }

        if (string.IsNullOrWhiteSpace(video.Url))
        {
            throw new ApplicationException($"Grok {operation} did not return a video URL.");
        }

        return video.Url;
    }

    /// <summary>
    /// Runs an xAI API call, translating gRPC failures into exceptions that carry the
    /// message the API actually reported.
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="call">API call to run</param>
    /// <param name="operation">Operation name used in error messages</param>
    /// <returns>Response from the API</returns>
    /// <exception cref="GrokContentBlockedException">The request was rejected by content moderation</exception>
    private async Task<T> CallGrokAsync<T>(Func<Task<T>> call, string operation)
    {
        try
        {
            return await call();
        }
        catch (RpcException ex)
        {
            var detail = string.IsNullOrWhiteSpace(ex.Status.Detail)
                ? ex.StatusCode.ToString()
                : ex.Status.Detail;

            logger?.LogError(ex, "Grok {Operation} failed with status {StatusCode}: {Detail}", operation,
                ex.StatusCode, detail);

            if (IsContentModerationRejection(ex))
            {
                throw new GrokContentBlockedException($"Grok blocked this {operation}: {detail}", ex);
            }

            throw new ApplicationException($"Grok {operation} failed: {detail}", ex);
        }
    }

    /// <summary>
    /// Determines whether a gRPC failure is xAI rejecting the request on content grounds
    /// rather than a transport or service problem.
    /// </summary>
    /// <param name="ex">gRPC exception</param>
    /// <returns>True if the request was rejected on content grounds</returns>
    internal static bool IsContentModerationRejection(RpcException ex)
    {
        if (ex.StatusCode != StatusCode.InvalidArgument && ex.StatusCode != StatusCode.PermissionDenied)
        {
            return false;
        }

        var detail = ex.Status.Detail ?? string.Empty;

        return detail.Contains("moderation", StringComparison.OrdinalIgnoreCase)
               || detail.Contains("blocked", StringComparison.OrdinalIgnoreCase)
               || detail.Contains("content policy", StringComparison.OrdinalIgnoreCase)
               || detail.Contains("safety", StringComparison.OrdinalIgnoreCase)
               || detail.Contains("violat", StringComparison.OrdinalIgnoreCase);
    }
}
