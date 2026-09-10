using System.Collections.Generic;
using MariBot.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MariBot.Core.Tests
{
    public class WolframAlphaServiceTests
    {
        /// <summary>
        /// A syntactically valid app id: the library wants exactly 17 characters.
        /// Nothing here talks to Wolfram, so it does not have to be a real one.
        /// </summary>
        private const string WellFormedAppId = "AAAAAA-AAAAAAAAAA";

        private static WolframAlphaService Service(string? appId)
        {
            var settings = new Dictionary<string, string?>();
            if (appId != null)
            {
                settings["DiscordSettings:WolframAlphaAppId"] = appId;
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            return new WolframAlphaService(NullLogger<WolframAlphaService>.Instance, configuration);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("SET-ME")]
        [InlineData("too-short")]
        [InlineData("far-too-long-to-be-a-real-wolfram-app-id")]
        public void ConstructingWithAnUnusableAppIdDoesNotThrow(string? appId)
        {
            // The point of the whole change: Discord.Net resolves this service
            // while building its command list at startup, so a throw here does
            // not disable one command, it stops the bot from starting.
            var service = Service(appId);

            Assert.False(service.IsConfigured);
        }

        [Fact]
        public void AWellFormedAppIdIsAccepted()
        {
            Assert.True(Service(WellFormedAppId).IsConfigured);
        }

        [Fact]
        public async Task QueryingWithoutAnAppIdFailsWithSomethingActionable()
        {
            var service = Service(null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.QuerySimple("2+2"));

            Assert.Contains("WolframAlphaAppId", exception.Message);
        }
    }
}
