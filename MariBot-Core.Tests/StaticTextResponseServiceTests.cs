using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using MariBot.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MariBot.Core.Tests
{
    public class StaticTextResponseServiceTests
    {
        private static readonly ulong Guild1 = 111111111111111111UL;
        private static readonly ulong Guild2 = 222222222222222222UL;

        private StaticTextResponseService CreateService()
        {
            var dataService = new DataService(NullLogger<DataService>.Instance, ":memory:");
            var commandService = new CommandService();
            var discordClient = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = Discord.GatewayIntents.None });
            var interactionService = new InteractionService(discordClient);
            return new StaticTextResponseService(dataService, commandService, interactionService);
        }

        // --- Blank validation: AddNewResponse ---

        [Fact]
        public async Task AddNewResponse_BlankCommandName_ReturnsError()
        {
            var service = CreateService();
            var result = await service.AddNewResponse("   ", "hello", Guild1, null, false);
            Assert.Equal("Command name cannot be blank.", result);
        }

        [Fact]
        public async Task AddNewResponse_BlankTextAndNoAttachments_ReturnsError()
        {
            var service = CreateService();
            var result = await service.AddNewResponse("testcmd", "", Guild1, null, false);
            Assert.Equal("Response cannot be blank. Provide text or attach a file.", result);
        }

        [Fact]
        public async Task AddNewResponse_WhitespaceTextAndNoAttachments_ReturnsError()
        {
            var service = CreateService();
            var result = await service.AddNewResponse("testcmd", "   ", Guild1, null, false);
            Assert.Equal("Response cannot be blank. Provide text or attach a file.", result);
        }

        // --- Blank validation: UpdateResponse ---

        [Fact]
        public async Task UpdateResponse_BlankCommandName_ReturnsError()
        {
            var service = CreateService();
            var result = await service.UpdateResponse("", "hello", Guild1, null, false);
            Assert.Equal("Command name cannot be blank.", result);
        }

        [Fact]
        public async Task UpdateResponse_BlankTextAndNoAttachments_ReturnsError()
        {
            var service = CreateService();
            var result = await service.UpdateResponse("testcmd", "", Guild1, null, false);
            Assert.Equal("Response cannot be blank. Provide text or attach a file.", result);
        }

        // --- Duplicate detection ---

        [Fact]
        public async Task AddNewResponse_DuplicateGuildCommand_ReturnsExplicitAlreadyExistsMessage()
        {
            var service = CreateService();
            await service.AddNewResponse("testcmd", "first", Guild1, null, false);
            var result = await service.AddNewResponse("testcmd", "second", Guild1, null, false);
            Assert.Equal("Failed to add command. A guild command with this name already exists.", result);
        }

        [Fact]
        public async Task AddNewResponse_DuplicateGlobalCommand_ReturnsExplicitAlreadyExistsMessage()
        {
            var service = CreateService();
            await service.AddNewResponse("globalcmd", "first", Guild1, null, true);
            var result = await service.AddNewResponse("globalcmd", "second", Guild1, null, true);
            Assert.Equal("Failed to add command. A global command with this name already exists.", result);
        }

        // --- Normal add/update ---

        [Fact]
        public async Task AddNewResponse_ValidGuildCommand_ReturnsOk()
        {
            var service = CreateService();
            var result = await service.AddNewResponse("testcmd", "hello world", Guild1, null, false);
            Assert.Equal("OK", result);
        }

        [Fact]
        public async Task AddNewResponse_ValidGlobalCommand_ReturnsOk()
        {
            var service = CreateService();
            var result = await service.AddNewResponse("globalcmd", "global response", Guild1, null, true);
            Assert.Equal("OK", result);
        }

        [Fact]
        public async Task UpdateResponse_CommandNotYetExisting_CreatesNewEntry()
        {
            var service = CreateService();
            var result = await service.UpdateResponse("newcmd", "new content", Guild1, null, false);
            Assert.Equal("OK", result);
            Assert.NotNull(service.GetResponse("newcmd", Guild1));
        }

        // --- PromoteToGlobal ---

        [Fact]
        public async Task PromoteToGlobal_HappyPath_ReturnsOk()
        {
            var service = CreateService();
            await service.AddNewResponse("promoteme", "hello", Guild1, null, false);
            var result = service.PromoteToGlobal("promoteme", Guild1);
            Assert.Equal("OK", result);
        }

        [Fact]
        public async Task PromoteToGlobal_HappyPath_GlobalEntryCreated()
        {
            var service = CreateService();
            await service.AddNewResponse("promoteme", "hello", Guild1, null, false);
            service.PromoteToGlobal("promoteme", Guild1);
            var globalEntries = service.GetGlobalResponses();
            Assert.Contains(globalEntries, r => r.Command.Equals("promoteme", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task PromoteToGlobal_HappyPath_GuildEntryRemoved()
        {
            var service = CreateService();
            await service.AddNewResponse("promoteme", "hello", Guild1, null, false);
            service.PromoteToGlobal("promoteme", Guild1);
            var guildEntries = service.GetGuildResponses(Guild1);
            Assert.DoesNotContain(guildEntries, r => r.Command.Equals("promoteme", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void PromoteToGlobal_CommandNotInGuild_ReturnsError()
        {
            var service = CreateService();
            var result = service.PromoteToGlobal("doesnotexist", Guild1);
            Assert.Equal("Command \"doesnotexist\" not found in this guild's responses.", result);
        }

        [Fact]
        public async Task PromoteToGlobal_GlobalAlreadyExists_ReturnsError()
        {
            var service = CreateService();
            await service.AddNewResponse("cmd", "guild version", Guild1, null, false);
            await service.AddNewResponse("cmd", "global version", Guild1, null, true);
            var result = service.PromoteToGlobal("cmd", Guild1);
            Assert.Equal("Failed to promote. A global command named \"cmd\" already exists.", result);
        }

        [Fact]
        public async Task PromoteToGlobal_OtherGuildHasSameCommand_ReturnsConflictMessage()
        {
            var service = CreateService();
            await service.AddNewResponse("cmd", "guild1 version", Guild1, null, false);
            await service.AddNewResponse("cmd", "guild2 version", Guild2, null, false);
            var result = service.PromoteToGlobal("cmd", Guild1);
            Assert.StartsWith("Failed to promote. Other guilds have the same command:", result);
            Assert.Contains(Guild2.ToString(), result);
        }

        // --- Filtering helpers ---

        [Fact]
        public async Task GetGlobalResponses_ReturnsOnlyGlobalEntries()
        {
            var service = CreateService();
            await service.AddNewResponse("globalcmd", "global", Guild1, null, true);
            await service.AddNewResponse("guildcmd", "guild", Guild1, null, false);
            var globals = service.GetGlobalResponses();
            Assert.All(globals, r => Assert.True(r.IsGlobal));
            Assert.Contains(globals, r => r.Command.Equals("globalcmd", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetGuildResponses_ReturnsOnlyMatchingGuildEntries()
        {
            var service = CreateService();
            await service.AddNewResponse("g1cmd", "for guild1", Guild1, null, false);
            await service.AddNewResponse("g2cmd", "for guild2", Guild2, null, false);
            var guild1Entries = service.GetGuildResponses(Guild1);
            Assert.All(guild1Entries, r => Assert.Equal(Guild1, r.GuildId));
            Assert.Contains(guild1Entries, r => r.Command.Equals("g1cmd", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetGuildResponses_DoesNotReturnOtherGuildsEntries()
        {
            var service = CreateService();
            await service.AddNewResponse("g2cmd", "for guild2", Guild2, null, false);
            var guild1Entries = service.GetGuildResponses(Guild1);
            Assert.DoesNotContain(guild1Entries, r => r.Command.Equals("g2cmd", StringComparison.OrdinalIgnoreCase));
        }
    }
}
