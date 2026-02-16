using Discord;
using Discord.Commands;
using MariBot.Core.Modules.Text;
using MariBot.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MariBot.Core.Tests
{
    public class AiModuleTests
    {
        private readonly Mock<GrokService> mockGrokService;
        private readonly Mock<ImageService> mockImageService;
        private readonly Mock<IMessageChannel> mockChannel;
        private readonly AiModule module;

        public AiModuleTests()
        {
            mockGrokService = new Mock<GrokService>();
            mockImageService = new Mock<ImageService>();
            mockChannel = new Mock<IMessageChannel>();

            var mockMessage = new Mock<IUserMessage>();
            mockMessage.Setup(m => m.Id).Returns(12345UL);

            // SendMessageAsync: text, isTTS, embed, options, allowedMentions, messageReference, components, stickers, embeds, flags, poll
            mockChannel.Setup(c => c.SendMessageAsync(
                    It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Embed>(),
                    It.IsAny<RequestOptions>(), It.IsAny<AllowedMentions>(),
                    It.IsAny<MessageReference>(), It.IsAny<MessageComponent>(),
                    It.IsAny<ISticker[]>(), It.IsAny<Embed[]>(),
                    It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()))
                .ReturnsAsync(mockMessage.Object);

            // SendFileAsync (Stream): stream, filename, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference, components, stickers, embeds, flags, poll
            mockChannel.Setup(c => c.SendFileAsync(
                    It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                    It.IsAny<bool>(), It.IsAny<AllowedMentions>(),
                    It.IsAny<MessageReference>(), It.IsAny<MessageComponent>(),
                    It.IsAny<ISticker[]>(), It.IsAny<Embed[]>(),
                    It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()))
                .ReturnsAsync(mockMessage.Object);

            var mockContext = new Mock<ICommandContext>();
            mockContext.Setup(c => c.Channel).Returns(mockChannel.Object);
            mockContext.Setup(c => c.Message).Returns(mockMessage.Object);

            var logger = NullLogger<AiModule>.Instance;

            module = new AiModule(null!, null!, mockImageService.Object, null!, logger, mockGrokService.Object);
            ((IModuleBase)module).SetContext(mockContext.Object);
        }

        // --- GrokTextGeneration ---

        [Fact]
        public async Task GrokTextGeneration_NormalResponse_SendsCodeBlock()
        {
            mockGrokService.Setup(s => s.GetGrokResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("Hello from Grok");

            await module.GrokTextGeneration("test prompt");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s.Contains("Hello from Grok") && s.StartsWith("```") && s.EndsWith("```")),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task GrokTextGeneration_LongResponse_TruncatesTo1990()
        {
            var longResponse = new string('A', 3000);
            mockGrokService.Setup(s => s.GetGrokResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(longResponse);

            await module.GrokTextGeneration("test prompt");

            // 1990 chars + "```\n" (4) + "\n```" (4) = 1998 max
            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => !s.Contains(longResponse) && s.Length <= 1998),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task GrokTextGeneration_ResponseWithBackticks_StripsBackticks()
        {
            mockGrokService.Setup(s => s.GetGrokResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("some ```code``` here");

            await module.GrokTextGeneration("test prompt");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s == "```\nsome code here\n```"),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task GrokTextGeneration_ServiceThrows_SendsErrorMessage()
        {
            mockGrokService.Setup(s => s.GetGrokResponseAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Grok API error"));

            await module.GrokTextGeneration("test prompt");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s.Contains("Something went wrong") && s.Contains("Grok API error")),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        // --- GrokImageGeneration ---

        [Fact]
        public async Task GrokImageGeneration_NormalFlow_SendsFile()
        {
            var fakeStream = new MemoryStream(new byte[] { 1, 2, 3 });
            mockGrokService.Setup(s => s.GetGrokImageAsync(It.IsAny<string>()))
                .ReturnsAsync("https://example.com/image.png");
            mockImageService.Setup(s => s.GetWebResource("https://example.com/image.png"))
                .ReturnsAsync(fakeStream);

            await module.GrokImageGeneration("test prompt");

            mockChannel.Verify(c => c.SendFileAsync(
                It.IsAny<Stream>(), It.Is<string>(s => s == "grok_image.png"),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Embed>(),
                It.IsAny<RequestOptions>(), It.IsAny<bool>(), It.IsAny<AllowedMentions>(),
                It.IsAny<MessageReference>(), It.IsAny<MessageComponent>(),
                It.IsAny<ISticker[]>(), It.IsAny<Embed[]>(),
                It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task GrokImageGeneration_GrokServiceThrows_SendsErrorMessage()
        {
            mockGrokService.Setup(s => s.GetGrokImageAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Image generation failed"));

            await module.GrokImageGeneration("test prompt");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s.Contains("Something went wrong") && s.Contains("Image generation failed")),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task GrokImageGeneration_ImageServiceThrows_SendsErrorMessage()
        {
            mockGrokService.Setup(s => s.GetGrokImageAsync(It.IsAny<string>()))
                .ReturnsAsync("https://example.com/image.png");
            mockImageService.Setup(s => s.GetWebResource(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Download failed"));

            await module.GrokImageGeneration("test prompt");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s.Contains("Something went wrong") && s.Contains("Download failed")),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }
        // --- GrokVideoGeneration ---

        [Fact]
        public async Task GrokVideoGeneration_NormalFlow_SendsFile()
        {
            var fakeStream = new MemoryStream(new byte[] { 1, 2, 3 });
            mockGrokService.Setup(s => s.GetGrokVideoAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync("https://example.com/video.mp4");
            mockImageService.Setup(s => s.GetWebResource("https://example.com/video.mp4"))
                .ReturnsAsync(fakeStream);

            await module.GrokVideoGeneration(5, "test prompt");

            mockChannel.Verify(c => c.SendFileAsync(
                It.IsAny<Stream>(), It.Is<string>(s => s == "grok_video.mp4"),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Embed>(),
                It.IsAny<RequestOptions>(), It.IsAny<bool>(), It.IsAny<AllowedMentions>(),
                It.IsAny<MessageReference>(), It.IsAny<MessageComponent>(),
                It.IsAny<ISticker[]>(), It.IsAny<Embed[]>(),
                It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task GrokVideoGeneration_SecondsTooLow_SendsValidationError()
        {
            await module.GrokVideoGeneration(0, "test prompt");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s.Contains("Duration must be between 1 and 10 seconds")),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
            mockGrokService.Verify(s => s.GetGrokVideoAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GrokVideoGeneration_SecondsTooHigh_SendsValidationError()
        {
            await module.GrokVideoGeneration(11, "test prompt");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s.Contains("Duration must be between 1 and 10 seconds")),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
            mockGrokService.Verify(s => s.GetGrokVideoAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GrokVideoGeneration_GrokServiceThrows_SendsErrorMessage()
        {
            mockGrokService.Setup(s => s.GetGrokVideoAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Video generation failed"));

            await module.GrokVideoGeneration(5, "test prompt");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s.Contains("Something went wrong") && s.Contains("Video generation failed")),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task GrokVideoGeneration_ImageServiceThrows_SendsErrorMessage()
        {
            mockGrokService.Setup(s => s.GetGrokVideoAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync("https://example.com/video.mp4");
            mockImageService.Setup(s => s.GetWebResource(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Download failed"));

            await module.GrokVideoGeneration(5, "test prompt");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s.Contains("Something went wrong") && s.Contains("Download failed")),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }
    }
}
