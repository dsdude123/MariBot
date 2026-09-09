using System;
using System.Threading.Tasks;
using MariBot.Core.Models;
using Moq;
using Xunit;

namespace MariBot.Core.Tests
{
    /// <summary>
    /// Which OpenAI model each command runs on, and that the commands whose
    /// models OpenAI discontinued still answer.
    /// </summary>
    public class AiModelUpgradeTests : AiModuleTestBase
    {
        [Fact]
        public async Task Gpt3WarnsThatItIsRetiredAndAnswersAnyway()
        {
            MockOpenAi.Setup(s => s.ExecuteGptQuery(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OpenAiModel>()))
                .ReturnsAsync("an answer");

            await Module.Gpt3TextCompletion("hello");

            VerifyMessageSent(t => t.Contains("gpt3") && t.Contains("retired"), Times.Once());
            // Retired means the model, not the command: the person still gets
            // their answer, from gpt4's model.
            MockOpenAi.Verify(s => s.ExecuteGptQuery("hello", It.IsAny<string>(), OpenAiModel.GPT4), Times.Once());
            VerifyMessageSent(t => t.Contains("an answer"), Times.Once());
        }

        [Fact]
        public async Task DalleWarnsThatItIsRetiredAndGeneratesAnyway()
        {
            MockOpenAi.Setup(s => s.ExecuteGptQuery(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OpenAiModel>()))
                .ReturnsAsync("https://example.invalid/image.png");
            MockImage.Setup(s => s.GetWebResource(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("no network in tests"));

            await Module.Dalle("a cat");

            // dall-e-3 was shut down in May 2026, so this command had stopped
            // working entirely. It now says so and runs on the image model.
            VerifyMessageSent(t => t.Contains("dalle") && t.Contains("retired"), Times.Once());
            MockOpenAi.Verify(s => s.ExecuteGptQuery("a cat", It.IsAny<string>(), OpenAiModel.GPTIMAGE), Times.Once());
        }

        [Fact]
        public async Task LiveCommandsDoNotClaimToBeRetired()
        {
            MockOpenAi.Setup(s => s.ExecuteGptQuery(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OpenAiModel>()))
                .ReturnsAsync("an answer");

            await Module.Gpt4TextCompletion("hello");
            await Module.Gpt5TextCompletion("hello");

            VerifyMessageSent(t => t.Contains("retired"), Times.Never());
            MockOpenAi.Verify(s => s.ExecuteGptQuery("hello", It.IsAny<string>(), OpenAiModel.GPT4), Times.Once());
            MockOpenAi.Verify(s => s.ExecuteGptQuery("hello", It.IsAny<string>(), OpenAiModel.GPT5), Times.Once());
        }
    }
}
