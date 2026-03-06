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
        // --- GrokImageEdit ---

        private AiModule CreateModuleWithAttachments(params IAttachment[] attachments)
        {
            var mockMsg = new Mock<IUserMessage>();
            mockMsg.Setup(m => m.Id).Returns(12345UL);
            mockMsg.Setup(m => m.Attachments).Returns(attachments);
            mockMsg.Setup(m => m.Reference).Returns((MessageReference)null!);

            var mockCtx = new Mock<ICommandContext>();
            mockCtx.Setup(c => c.Channel).Returns(mockChannel.Object);
            mockCtx.Setup(c => c.Message).Returns(mockMsg.Object);

            var mod = new AiModule(null!, null!, mockImageService.Object, null!, NullLogger<AiModule>.Instance, mockGrokService.Object);
            ((IModuleBase)mod).SetContext(mockCtx.Object);
            return mod;
        }

        private static IAttachment CreateMockAttachment(string url, string contentType)
        {
            var mock = new Mock<IAttachment>();
            mock.Setup(a => a.Url).Returns(url);
            mock.Setup(a => a.ContentType).Returns(contentType);
            return mock.Object;
        }

        [Fact]
        public async Task GrokImageEdit_OneAttachment_SendsEditedFile()
        {
            var attachment = CreateMockAttachment("https://example.com/photo.png", "image/png");
            var mod = CreateModuleWithAttachments(attachment);

            var fakeStream = new MemoryStream(new byte[] { 1, 2, 3 });
            mockGrokService.Setup(s => s.GetGrokImageEditAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
                .ReturnsAsync("https://example.com/edited.png");
            mockImageService.Setup(s => s.GetWebResource("https://example.com/edited.png"))
                .ReturnsAsync(fakeStream);

            await mod.GrokImageEdit("make it blue");

            mockGrokService.Verify(s => s.GetGrokImageEditAsync("make it blue",
                It.Is<List<string>>(l => l.Count == 1 && l[0] == "https://example.com/photo.png")), Times.Once);
            mockChannel.Verify(c => c.SendFileAsync(
                It.IsAny<Stream>(), It.Is<string>(s => s == "grok_edit.png"),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Embed>(),
                It.IsAny<RequestOptions>(), It.IsAny<bool>(), It.IsAny<AllowedMentions>(),
                It.IsAny<MessageReference>(), It.IsAny<MessageComponent>(),
                It.IsAny<ISticker[]>(), It.IsAny<Embed[]>(),
                It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task GrokImageEdit_TwoAttachments_SendsEditedFile()
        {
            var attachment1 = CreateMockAttachment("https://example.com/photo1.png", "image/png");
            var attachment2 = CreateMockAttachment("https://example.com/photo2.jpg", "image/jpeg");
            var mod = CreateModuleWithAttachments(attachment1, attachment2);

            var fakeStream = new MemoryStream(new byte[] { 1, 2, 3 });
            mockGrokService.Setup(s => s.GetGrokImageEditAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
                .ReturnsAsync("https://example.com/edited.png");
            mockImageService.Setup(s => s.GetWebResource("https://example.com/edited.png"))
                .ReturnsAsync(fakeStream);

            await mod.GrokImageEdit("combine these");

            mockGrokService.Verify(s => s.GetGrokImageEditAsync("combine these",
                It.Is<List<string>>(l => l.Count == 2 && l[0] == "https://example.com/photo1.png" && l[1] == "https://example.com/photo2.jpg")), Times.Once);
            mockChannel.Verify(c => c.SendFileAsync(
                It.IsAny<Stream>(), It.Is<string>(s => s == "grok_edit.png"),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Embed>(),
                It.IsAny<RequestOptions>(), It.IsAny<bool>(), It.IsAny<AllowedMentions>(),
                It.IsAny<MessageReference>(), It.IsAny<MessageComponent>(),
                It.IsAny<ISticker[]>(), It.IsAny<Embed[]>(),
                It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task GrokImageEdit_NoImages_SendsErrorMessage()
        {
            var mod = CreateModuleWithAttachments();

            await mod.GrokImageEdit("make it blue");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s.Contains("Please provide at least one image to edit")),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
            mockGrokService.Verify(s => s.GetGrokImageEditAsync(It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
        }

        [Fact]
        public async Task GrokImageEdit_UnsupportedImageType_FiltersOut()
        {
            var gifAttachment = CreateMockAttachment("https://example.com/anim.gif", "image/gif");
            var pngAttachment = CreateMockAttachment("https://example.com/photo.png", "image/png");
            var mod = CreateModuleWithAttachments(gifAttachment, pngAttachment);

            var fakeStream = new MemoryStream(new byte[] { 1, 2, 3 });
            mockGrokService.Setup(s => s.GetGrokImageEditAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
                .ReturnsAsync("https://example.com/edited.png");
            mockImageService.Setup(s => s.GetWebResource("https://example.com/edited.png"))
                .ReturnsAsync(fakeStream);

            await mod.GrokImageEdit("make it blue");

            mockGrokService.Verify(s => s.GetGrokImageEditAsync("make it blue",
                It.Is<List<string>>(l => l.Count == 1 && l[0] == "https://example.com/photo.png")), Times.Once);
        }

        [Fact]
        public async Task GrokImageEdit_GrokServiceThrows_SendsErrorMessage()
        {
            var attachment = CreateMockAttachment("https://example.com/photo.png", "image/png");
            var mod = CreateModuleWithAttachments(attachment);

            mockGrokService.Setup(s => s.GetGrokImageEditAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
                .ThrowsAsync(new Exception("Edit failed"));

            await mod.GrokImageEdit("make it blue");

            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s.Contains("Something went wrong") && s.Contains("Edit failed")),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task GrokImageEdit_ImageServiceThrows_SendsErrorMessage()
        {
            var attachment = CreateMockAttachment("https://example.com/photo.png", "image/png");
            var mod = CreateModuleWithAttachments(attachment);

            mockGrokService.Setup(s => s.GetGrokImageEditAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
                .ReturnsAsync("https://example.com/edited.png");
            mockImageService.Setup(s => s.GetWebResource(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Download failed"));

            await mod.GrokImageEdit("make it blue");

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
