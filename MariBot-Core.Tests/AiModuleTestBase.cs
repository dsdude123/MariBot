using Discord;
using Discord.Commands;
using MariBot.Core.Modules.Text;
using MariBot.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MariBot.Core.Tests
{
    /// <summary>
    /// An <see cref="AiModule"/> wired to mocks, with just enough Discord
    /// context for a command to run.
    /// </summary>
    public abstract class AiModuleTestBase
    {
        protected readonly Mock<OpenAiService> MockOpenAi = new();
        protected readonly Mock<FluxService> MockFlux = new();
        protected readonly Mock<ImageService> MockImage = new();
        protected readonly Mock<IMessageChannel> MockChannel = new();
        protected readonly AiModule Module;

        protected AiModuleTestBase()
        {
            var mockMessage = new Mock<IUserMessage>();
            mockMessage.Setup(m => m.Id).Returns(12345UL);

            MockChannel.Setup(c => c.SendMessageAsync(
                    It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Embed>(),
                    It.IsAny<RequestOptions>(), It.IsAny<AllowedMentions>(),
                    It.IsAny<MessageReference>(), It.IsAny<MessageComponent>(),
                    It.IsAny<ISticker[]>(), It.IsAny<Embed[]>(),
                    It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()))
                .ReturnsAsync(mockMessage.Object);

            // Every OpenAI command reads Context.User.Id to tag the request, so a
            // context without one throws before reaching the service.
            var mockUser = new Mock<IUser>();
            mockUser.Setup(u => u.Id).Returns(67890UL);

            var mockContext = new Mock<ICommandContext>();
            mockContext.Setup(c => c.Channel).Returns(MockChannel.Object);
            mockContext.Setup(c => c.Message).Returns(mockMessage.Object);
            mockContext.Setup(c => c.User).Returns(mockUser.Object);

            Module = new AiModule(MockOpenAi.Object, null!, MockImage.Object, MockFlux.Object,
                NullLogger<AiModule>.Instance, null!);
            ((IModuleBase)Module).SetContext(mockContext.Object);
        }

        /// <summary>
        /// Asserts how many messages matching <paramref name="matches"/> were posted.
        /// </summary>
        protected void VerifyMessageSent(System.Func<string, bool> matches, Times times)
        {
            MockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(text => text != null && matches(text)),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(), It.IsAny<Embed[]>(),
                It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()), times);
        }
    }
}
