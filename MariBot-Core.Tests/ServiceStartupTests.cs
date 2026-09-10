using System;
using System.Collections.Generic;
using MariBot.Core.Services;
using MariBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MariBot.Core.Tests
{
    /// <summary>
    /// Every service that reads a credential must survive being constructed
    /// without one.
    /// </summary>
    /// <remarks>
    /// Not a theoretical concern: Discord.Net resolves the whole service graph
    /// while it builds its command list during startup, so a constructor that
    /// throws over a missing optional credential does not disable one command —
    /// it stops the bot from starting at all. Both of the ways an operator
    /// arrives at "no credential" are covered, because they reach the services
    /// differently: absent (the key is not in configuration) and empty (it is,
    /// but blank — which is what copying .env.example without filling it in
    /// produces, since compose passes a blank value through as an empty string).
    /// The shipped SET-ME placeholder is covered for the same reason.
    /// </remarks>
    public class ServiceStartupTests
    {
        public static TheoryData<string, string?> UnusableValues => new()
        {
            { "absent", null },
            { "empty", "" },
            { "whitespace", "   " },
            { "placeholder", "SET-ME" }
        };

        private static IConfiguration Configuration(string? value)
        {
            var settings = new Dictionary<string, string?>
            {
                ["DiscordSettings:GoogleCloudKey"] = value,
                ["DiscordSettings:GoogleCustomSearchId"] = value,
                ["DiscordSettings:OpenAiApiKey"] = value,
                ["DiscordSettings:OpenAiOrganization"] = value,
                ["DiscordSettings:WolframAlphaAppId"] = value,
                ["DiscordSettings:BlackForestLabsApiKey"] = value,
                ["DiscordSettings:IGDBClientId"] = value,
                ["DiscordSettings:IGDBClientSecret"] = value,
                // Nothing should try to refresh a 14 MB catalog during a test.
                ["DiscordSettings:SleeperCacheAutoRefresh"] = "false"
            };

            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        private static DataService InMemoryData() =>
            new(NullLogger<DataService>.Instance, ":memory:");

        [Theory]
        [MemberData(nameof(UnusableValues))]
        public void OpenAiServiceStartsWithoutAKey(string label, string? value)
        {
            var service = new OpenAiService(Configuration(value), NullLogger<OpenAiService>.Instance, InMemoryData());

            Assert.False(service.IsConfigured, label);
        }

        [Fact]
        public async Task AnUnconfiguredOpenAiQueryFailsWithSomethingActionable()
        {
            var service = new OpenAiService(
                Configuration(null), NullLogger<OpenAiService>.Instance, InMemoryData());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ExecuteGptQuery("hello", "1", Models.OpenAiModel.GPT4));

            Assert.Contains("OpenAiApiKey", exception.Message);
        }

        [Theory]
        [MemberData(nameof(UnusableValues))]
        public void IgdbServiceStartsWithoutCredentials(string label, string? value)
        {
            var service = new IgdbService(Configuration(value), NullLogger<IgdbService>.Instance);

            Assert.False(service.IsConfigured, label);
        }

        [Fact]
        public async Task AnUnconfiguredIgdbLookupReturnsNoMatchRatherThanThrowing()
        {
            var service = new IgdbService(Configuration(null), NullLogger<IgdbService>.Instance);

            Assert.Null(await service.SearchGame("chrono trigger"));
        }

        [Theory]
        [MemberData(nameof(UnusableValues))]
        public void WolframAlphaServiceStartsWithoutAnAppId(string label, string? value)
        {
            var service = new WolframAlphaService(
                NullLogger<WolframAlphaService>.Instance, Configuration(value));

            Assert.False(service.IsConfigured, label);
        }

        /// <summary>
        /// The services that were already safe, kept here so they stay that way.
        /// Each one builds a third-party client from configuration, which is the
        /// shape that bites.
        /// </summary>
        [Theory]
        [MemberData(nameof(UnusableValues))]
        public void OtherCredentialReadingServicesStartWithoutTheirCredentials(string label, string? value)
        {
            var configuration = Configuration(value);

            var exception = Record.Exception(() =>
            {
                _ = new FluxService(configuration, NullLogger<FluxService>.Instance);
                _ = new GoogleService(configuration);
                _ = new PricechartingService(NullLogger<PricechartingService>.Instance);
                _ = new SleeperPlayerCache(configuration, NullLogger<SleeperPlayerCache>.Instance);
            });

            Assert.Null(exception);
        }
    }
}
