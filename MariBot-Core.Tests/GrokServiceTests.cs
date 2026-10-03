using Grpc.Core;
using MariBot.Core.Models.Grok;
using MariBot.Core.Services;
using Xunit;

namespace MariBot.Core.Tests
{
    public class GrokServiceTests
    {
        // --- ResolveImageUrl ---

        [Fact]
        public void ResolveImageUrl_ModerationRespected_ReturnsUrl()
        {
            var response = new XaiApi.ImageResponse
            {
                Images =
                {
                    new XaiApi.GeneratedImage { Url = "https://example.com/image.png", RespectModeration = true }
                }
            };

            Assert.Equal("https://example.com/image.png", GrokService.ResolveImageUrl(response, "image generation"));
        }

        [Fact]
        public void ResolveImageUrl_ModerationNotRespected_ThrowsContentBlocked()
        {
            // A blocked generation comes back as a placeholder with no URL rather than an API error.
            var response = new XaiApi.ImageResponse
            {
                Images =
                {
                    new XaiApi.GeneratedImage { Url = "", RespectModeration = false }
                }
            };

            var ex = Assert.Throws<GrokContentBlockedException>(
                () => GrokService.ResolveImageUrl(response, "image edit"));
            Assert.Contains("image edit", ex.Message);
            Assert.Contains("moderation", ex.Message);
        }

        [Fact]
        public void ResolveImageUrl_NoImages_ThrowsWithMeaningfulMessage()
        {
            var ex = Assert.Throws<ApplicationException>(
                () => GrokService.ResolveImageUrl(new XaiApi.ImageResponse(), "image generation"));
            Assert.Contains("did not return an image", ex.Message);
        }

        [Fact]
        public void ResolveImageUrl_EmptyUrl_ThrowsWithMeaningfulMessage()
        {
            var response = new XaiApi.ImageResponse
            {
                Images =
                {
                    new XaiApi.GeneratedImage { Url = "", RespectModeration = true }
                }
            };

            var ex = Assert.Throws<ApplicationException>(
                () => GrokService.ResolveImageUrl(response, "image generation"));
            Assert.Contains("did not return an image URL", ex.Message);
        }

        // --- ResolveVideoUrl ---

        [Fact]
        public void ResolveVideoUrl_ModerationRespected_ReturnsUrl()
        {
            var response = new XaiApi.VideoResponse
            {
                Video = new XaiApi.GeneratedVideo { Url = "https://example.com/video.mp4", RespectModeration = true }
            };

            Assert.Equal("https://example.com/video.mp4", GrokService.ResolveVideoUrl(response, "video generation"));
        }

        [Fact]
        public void ResolveVideoUrl_ModerationNotRespected_ThrowsContentBlocked()
        {
            var response = new XaiApi.VideoResponse
            {
                Video = new XaiApi.GeneratedVideo { Url = "", RespectModeration = false }
            };

            var ex = Assert.Throws<GrokContentBlockedException>(
                () => GrokService.ResolveVideoUrl(response, "video generation"));
            Assert.Contains("moderation", ex.Message);
        }

        [Fact]
        public void ResolveVideoUrl_NoVideo_ThrowsWithMeaningfulMessage()
        {
            var ex = Assert.Throws<ApplicationException>(
                () => GrokService.ResolveVideoUrl(null, "video generation"));
            Assert.Contains("did not return a video", ex.Message);
        }

        // --- IsContentModerationRejection ---

        [Theory]
        [InlineData(StatusCode.InvalidArgument, "Request blocked by content moderation", true)]
        [InlineData(StatusCode.PermissionDenied, "This prompt violates our content policy", true)]
        [InlineData(StatusCode.InvalidArgument, "Unknown model: grok-nope", false)]
        [InlineData(StatusCode.Unavailable, "Request blocked by content moderation", false)]
        [InlineData(StatusCode.Unauthenticated, "Invalid API key", false)]
        public void IsContentModerationRejection_ClassifiesByStatusAndDetail(StatusCode statusCode, string detail, bool expected)
        {
            var ex = new RpcException(new Status(statusCode, detail));

            Assert.Equal(expected, GrokService.IsContentModerationRejection(ex));
        }
    }
}
