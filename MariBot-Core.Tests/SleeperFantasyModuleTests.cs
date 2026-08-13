using Discord;
using Discord.Commands;
using MariBot.Core.Models;
using MariBot.Core.Models.Sleeper;
using MariBot.Core.Modules.Text;
using MariBot.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MariBot.Core.Tests
{
    public class SleeperFantasyModuleTests
    {
        private readonly Mock<DataService> mockDataService = new();
        private readonly Mock<SleeperService> mockSleeperService = new();
        private readonly Mock<IMessageChannel> mockChannel = new();
        private readonly Mock<IGuild> mockGuild = new();

        private const ulong GuildId = 410597263363276801;
        private const string LeagueId = "289646328504385536";

        public SleeperFantasyModuleTests()
        {
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

            mockChannel.Setup(c => c.Id).Returns(999UL);
            mockGuild.Setup(g => g.Id).Returns(GuildId);
        }

        /// <summary>
        /// Builds the module with a context that is either in a guild or in a DM.
        /// </summary>
        private SleeperFantasyModule CreateModule(bool inGuild = true)
        {
            var mockMessage = new Mock<IUserMessage>();
            mockMessage.Setup(m => m.Id).Returns(12345UL);

            var mockContext = new Mock<ICommandContext>();
            mockContext.Setup(c => c.Channel).Returns(mockChannel.Object);
            mockContext.Setup(c => c.Message).Returns(mockMessage.Object);
            mockContext.Setup(c => c.Guild).Returns(inGuild ? mockGuild.Object : null!);

            var module = new SleeperFantasyModule(mockDataService.Object, mockSleeperService.Object,
                NullLogger<SleeperFantasyModule>.Instance);
            ((IModuleBase)module).SetContext(mockContext.Object);
            return module;
        }

        private void VerifyTextReply(Func<string, bool> predicate, string because)
        {
            mockChannel.Verify(c => c.SendMessageAsync(
                It.Is<string>(s => s != null && predicate(s)),
                It.IsAny<bool>(), It.IsAny<Embed>(), It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once, because);
        }

        private Embed CaptureEmbed()
        {
            Embed? captured = null;
            mockChannel.Verify(c => c.SendMessageAsync(
                It.IsAny<string>(), It.IsAny<bool>(), It.Is<Embed>(e => Capture(e, ref captured)),
                It.IsAny<RequestOptions>(), It.IsAny<AllowedMentions>(),
                It.IsAny<MessageReference>(), It.IsAny<MessageComponent>(),
                It.IsAny<ISticker[]>(), It.IsAny<Embed[]>(),
                It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()),
                Times.Once);

            Assert.NotNull(captured);
            return captured!;
        }

        private static bool Capture(Embed embed, ref Embed? slot)
        {
            if (embed == null) return false;
            slot = embed;
            return true;
        }

        private void GivenSubscription()
        {
            mockDataService.Setup(d => d.GetSleeperSubscription(GuildId))
                .Returns(new SleeperSubscription { GuildId = GuildId, LeagueId = LeagueId });
        }

        private static SleeperService.LeagueContext Context(List<Roster> rosters, List<LeagueUser> users)
        {
            var league = new League { league_id = LeagueId, name = "Sleeper Friends League" };
            return new SleeperService.LeagueContext(league, rosters, users,
                SleeperService.BuildRosterNameMap(rosters, users));
        }

        private static Roster MakeRoster(int id, string ownerId, int wins, int losses, int fpts, int fptsDecimal)
        {
            return new Roster
            {
                roster_id = id,
                owner_id = ownerId,
                players = new List<string>(),
                starters = new List<string>(),
                settings = new Roster.RosterSettings
                {
                    wins = wins, losses = losses, ties = 0,
                    fpts = fpts, fpts_decimal = fptsDecimal
                }
            };
        }

        private static LeagueUser MakeUser(string id, string displayName, string? teamName = null)
        {
            return new LeagueUser
            {
                user_id = id,
                display_name = displayName,
                metadata = teamName == null ? null : new LeagueUser.UserMetadata { team_name = teamName }
            };
        }

        // --- Subscription guard ---

        [Fact]
        public async Task Standings_NoSubscription_RepliesWithSetupHint()
        {
            mockDataService.Setup(d => d.GetSleeperSubscription(It.IsAny<ulong>())).Returns((SleeperSubscription?)null);

            await CreateModule().GetStandings();

            VerifyTextReply(s => s.Contains("No Sleeper league is configured") && s.Contains("subscribe"),
                "an unconfigured guild should be told how to configure itself");
        }

        [Fact]
        public async Task Standings_InDirectMessage_RepliesGracefully()
        {
            // Yahoo's equivalent throws NullReferenceException here.
            await CreateModule(inGuild: false).GetStandings();

            VerifyTextReply(s => s.Contains("only work in a server"), "a DM should not throw");
            mockDataService.Verify(d => d.GetSleeperSubscription(It.IsAny<ulong>()), Times.Never);
        }

        [Fact]
        public async Task Subscribe_UnknownLeagueId_RepliesNotFound()
        {
            mockSleeperService.Setup(s => s.GetLeague(It.IsAny<string>()))
                .ThrowsAsync(new SleeperApiException(System.Net.HttpStatusCode.NotFound, "nope"));

            await CreateModule().Subscribe("garbage");

            VerifyTextReply(s => s.Contains("No Sleeper league with that ID"), "a bad id should not look like a crash");
            mockDataService.Verify(d => d.UpdateSleeperSubscription(It.IsAny<SleeperSubscription>()), Times.Never);
        }

        [Fact]
        public async Task Subscribe_ValidLeague_StoresGuildLeagueAndChannel()
        {
            mockSleeperService.Setup(s => s.GetLeague(LeagueId))
                .ReturnsAsync(new League { league_id = LeagueId, name = "Sleeper Friends League" });
            mockDataService.Setup(d => d.UpdateSleeperSubscription(It.IsAny<SleeperSubscription>())).Returns(true);

            await CreateModule().Subscribe(LeagueId);

            mockDataService.Verify(d => d.UpdateSleeperSubscription(It.Is<SleeperSubscription>(
                sub => sub.GuildId == GuildId
                       && sub.LeagueId == LeagueId
                       && sub.AnnouncementChannelId == 999UL)), Times.Once);
            VerifyTextReply(s => s.Contains("Sleeper Friends League"), "the confirmation should name the league");
        }

        // --- Standings ---

        [Fact]
        public async Task Standings_SortsByWinsThenLossesThenPoints()
        {
            GivenSubscription();

            var rosters = new List<Roster>
            {
                MakeRoster(1, "u1", wins: 5, losses: 5, fpts: 1000, fptsDecimal: 0),
                MakeRoster(2, "u2", wins: 8, losses: 2, fpts: 1200, fptsDecimal: 0),
                // Same record as roster 2, fewer points — ranks below it.
                MakeRoster(3, "u3", wins: 8, losses: 2, fpts: 1100, fptsDecimal: 0),
                // More wins than roster 1 but more losses than roster 3.
                MakeRoster(4, "u4", wins: 8, losses: 3, fpts: 1500, fptsDecimal: 0)
            };

            var users = new List<LeagueUser>
            {
                MakeUser("u1", "Alice"), MakeUser("u2", "Bob"),
                MakeUser("u3", "Carol"), MakeUser("u4", "Dave")
            };

            mockSleeperService.Setup(s => s.GetLeagueContext(LeagueId)).ReturnsAsync(Context(rosters, users));

            await CreateModule().GetStandings();

            var embed = CaptureEmbed();
            Assert.Equal(new[] { "1. Bob", "2. Carol", "3. Dave", "4. Alice" },
                embed.Fields.Select(f => f.Name).ToArray());
        }

        [Fact]
        public async Task Standings_CombinesTheSplitPointsFields()
        {
            GivenSubscription();

            // Sleeper sends 1776 + 6 to mean 1776.06, not 1776.6.
            var rosters = new List<Roster> { MakeRoster(1, "u1", 7, 6, fpts: 1776, fptsDecimal: 6) };
            rosters[0].settings.fpts_against = 1695;
            rosters[0].settings.fpts_against_decimal = 36;

            var users = new List<LeagueUser> { MakeUser("u1", "Alice", "Giant Dolphins") };

            mockSleeperService.Setup(s => s.GetLeagueContext(LeagueId)).ReturnsAsync(Context(rosters, users));

            await CreateModule().GetStandings();

            var embed = CaptureEmbed();
            var field = Assert.Single(embed.Fields);
            Assert.Equal("1. Giant Dolphins", field.Name);
            Assert.Contains("1776.06 PF", field.Value);
            Assert.Contains("1695.36 PA", field.Value);
            Assert.Contains("7-6-0", field.Value);
        }

        // --- Roster name resolution ---

        [Fact]
        public void RosterNames_PreferTeamNameThenDisplayNameThenRosterNumber()
        {
            var rosters = new List<Roster>
            {
                MakeRoster(1, "u1", 0, 0, 0, 0),
                MakeRoster(2, "u2", 0, 0, 0, 0),
                MakeRoster(3, "orphan", 0, 0, 0, 0)
            };

            var users = new List<LeagueUser>
            {
                MakeUser("u1", "Alice", "Giant Dolphins"),
                MakeUser("u2", "Bob")
            };

            var map = SleeperService.BuildRosterNameMap(rosters, users);

            Assert.Equal("Giant Dolphins", map[1]);
            Assert.Equal("Bob", map[2]);
            Assert.Equal("Roster 3", map[3]);
        }

        // --- Scoreboard pairing ---

        [Fact]
        public void PairMatchups_GroupsByMatchupId()
        {
            var matchups = new List<Matchup>
            {
                new() { roster_id = 1, matchup_id = 1, points = 100 },
                new() { roster_id = 2, matchup_id = 2, points = 90 },
                new() { roster_id = 3, matchup_id = 1, points = 110 },
                new() { roster_id = 4, matchup_id = 2, points = 80 }
            };

            var pairs = SleeperFantasyModule.PairMatchups(matchups);

            Assert.Equal(2, pairs.Count);
            Assert.Equal((1, 3), (pairs[0].Item1.roster_id, pairs[0].Item2!.roster_id));
            Assert.Equal((2, 4), (pairs[1].Item1.roster_id, pairs[1].Item2!.roster_id));
        }

        [Fact]
        public void PairMatchups_NullMatchupId_BecomesAnUnpairedEntry()
        {
            var matchups = new List<Matchup>
            {
                new() { roster_id = 1, matchup_id = 1, points = 100 },
                new() { roster_id = 3, matchup_id = 1, points = 110 },
                // A bye: Sleeper leaves matchup_id null.
                new() { roster_id = 5, matchup_id = null, points = 70 }
            };

            var pairs = SleeperFantasyModule.PairMatchups(matchups);

            Assert.Equal(2, pairs.Count);
            Assert.NotNull(pairs[0].Item2);
            Assert.Equal(5, pairs[1].Item1.roster_id);
            Assert.Null(pairs[1].Item2);
        }

        [Fact]
        public void PairMatchups_OddGroup_LeavesTheLastEntryUnpaired()
        {
            var matchups = new List<Matchup>
            {
                new() { roster_id = 1, matchup_id = 1 },
                new() { roster_id = 2, matchup_id = 1 },
                new() { roster_id = 3, matchup_id = 1 }
            };

            var pairs = SleeperFantasyModule.PairMatchups(matchups);

            Assert.Equal(2, pairs.Count);
            Assert.Null(pairs[1].Item2);
        }

        [Fact]
        public void PairMatchups_Null_ReturnsEmpty()
        {
            Assert.Empty(SleeperFantasyModule.PairMatchups(null));
        }

        [Fact]
        public async Task Scoreboard_RendersOneFieldPerPairing()
        {
            GivenSubscription();

            var rosters = new List<Roster> { MakeRoster(1, "u1", 0, 0, 0, 0), MakeRoster(2, "u2", 0, 0, 0, 0) };
            var users = new List<LeagueUser> { MakeUser("u1", "Alice"), MakeUser("u2", "Bob") };

            mockSleeperService.Setup(s => s.GetLeagueContext(LeagueId)).ReturnsAsync(Context(rosters, users));
            mockSleeperService.Setup(s => s.GetMatchups(LeagueId, 3)).ReturnsAsync(new List<Matchup>
            {
                new() { roster_id = 1, matchup_id = 1, points = 148.04 },
                new() { roster_id = 2, matchup_id = 1, points = 96.5 }
            });

            await CreateModule().GetScoreboard(3);

            var embed = CaptureEmbed();
            var field = Assert.Single(embed.Fields);
            Assert.Equal("Alice vs. Bob", field.Name);
            Assert.Equal("148.04 - 96.50", field.Value);
        }

        // --- Roster listing ---

        [Fact]
        public async Task Roster_LongBench_TruncatesRatherThanExceedingTheFieldLimit()
        {
            GivenSubscription();

            // 40 players with long names blows past Discord's 1024-character field cap.
            var players = Enumerable.Range(0, 40).Select(i => $"p{i}").ToList();
            var roster = MakeRoster(1, "u1", 0, 0, 0, 0);
            roster.players = players;
            roster.starters = new List<string>();

            var users = new List<LeagueUser> { MakeUser("u1", "Alice", "Giant Dolphins") };

            mockSleeperService.Setup(s => s.GetLeagueContext(LeagueId))
                .ReturnsAsync(Context(new List<Roster> { roster }, users));
            mockSleeperService.Setup(s => s.GetPlayer(It.IsAny<string>()))
                .Returns((string id) => new SleeperPlayer
                {
                    player_id = id,
                    full_name = "Bartholomew Featherstonehaugh " + id,
                    position = "WR",
                    team = "SEA"
                });

            await CreateModule().GetRoster("Giant Dolphins");

            var embed = CaptureEmbed();
            var bench = embed.Fields.Single(f => f.Name == "Bench");
            Assert.True(bench.Value.Length <= 1024, $"bench field was {bench.Value.Length} characters");
            Assert.Contains("…and", bench.Value);
        }

        [Fact]
        public async Task Roster_NoArgument_ListsTheTeams()
        {
            GivenSubscription();

            var rosters = new List<Roster> { MakeRoster(1, "u1", 0, 0, 0, 0), MakeRoster(2, "u2", 0, 0, 0, 0) };
            var users = new List<LeagueUser> { MakeUser("u1", "Alice", "Giant Dolphins"), MakeUser("u2", "Bob") };

            mockSleeperService.Setup(s => s.GetLeagueContext(LeagueId)).ReturnsAsync(Context(rosters, users));

            await CreateModule().GetRoster();

            var embed = CaptureEmbed();
            var teams = embed.Fields.Single(f => f.Name == "Teams");
            Assert.Contains("Giant Dolphins", teams.Value);
            Assert.Contains("Bob", teams.Value);
        }

        [Fact]
        public async Task Roster_AmbiguousMatch_ListsTheCandidates()
        {
            GivenSubscription();

            var rosters = new List<Roster> { MakeRoster(1, "u1", 0, 0, 0, 0), MakeRoster(2, "u2", 0, 0, 0, 0) };
            var users = new List<LeagueUser>
            {
                MakeUser("u1", "Alice", "Team Alpha"),
                MakeUser("u2", "Bob", "Team Beta")
            };

            mockSleeperService.Setup(s => s.GetLeagueContext(LeagueId)).ReturnsAsync(Context(rosters, users));

            await CreateModule().GetRoster("team");

            VerifyTextReply(s => s.Contains("more than one team") && s.Contains("Team Alpha") && s.Contains("Team Beta"),
                "an ambiguous match should list the candidates");
        }

        [Fact]
        public async Task Roster_NoMatch_SaysSo()
        {
            GivenSubscription();

            var rosters = new List<Roster> { MakeRoster(1, "u1", 0, 0, 0, 0) };
            var users = new List<LeagueUser> { MakeUser("u1", "Alice", "Team Alpha") };

            mockSleeperService.Setup(s => s.GetLeagueContext(LeagueId)).ReturnsAsync(Context(rosters, users));

            await CreateModule().GetRoster("nobody");

            VerifyTextReply(s => s.Contains("No team matching"), "an unmatched name should be reported");
        }

        // --- Bracket ---

        [Fact]
        public async Task Bracket_Empty_SaysNotAvailableYet()
        {
            GivenSubscription();

            mockSleeperService.Setup(s => s.GetLeagueContext(LeagueId))
                .ReturnsAsync(Context(new List<Roster>(), new List<LeagueUser>()));
            mockSleeperService.Setup(s => s.GetWinnersBracket(LeagueId)).ReturnsAsync(new List<BracketMatchup>());

            await CreateModule().GetBracket();

            VerifyTextReply(s => s.Contains("isn't available yet"), "an empty bracket should not render an empty embed");
        }

        [Fact]
        public void BracketMatch_PlayedMatch_MarksTheWinner()
        {
            var names = new Dictionary<int, string> { { 1, "Alice" }, { 5, "Bob" } };
            var match = new BracketMatchup { m = 1, r = 1, t1 = 5, t2 = 1, w = 1, l = 5 };

            Assert.Equal("Bob vs Alice ✅", SleeperFantasyModule.FormatBracketMatch(match, names));
        }

        [Fact]
        public void BracketMatch_UndecidedSlot_RendersTheFeedingMatch()
        {
            var names = new Dictionary<int, string> { { 6, "Alice" } };
            var match = new BracketMatchup
            {
                m = 3, r = 2, t1 = 6, t2 = null,
                t2_from = new BracketMatchup.BracketSource { w = 1 }
            };

            Assert.Equal("Alice vs Winner of M1", SleeperFantasyModule.FormatBracketMatch(match, names));
        }

        [Fact]
        public void BracketMatch_ConsolationSlot_RendersTheLoserSource()
        {
            var match = new BracketMatchup
            {
                m = 5, r = 2, t1 = null, t2 = null,
                t1_from = new BracketMatchup.BracketSource { l = 1 },
                t2_from = new BracketMatchup.BracketSource { l = 2 }
            };

            Assert.Equal("Loser of M1 vs Loser of M2",
                SleeperFantasyModule.FormatBracketMatch(match, new Dictionary<int, string>()));
        }

        // --- Trending ---

        [Fact]
        public async Task Trending_NeedsNoSubscription()
        {
            mockDataService.Setup(d => d.GetSleeperSubscription(It.IsAny<ulong>())).Returns((SleeperSubscription?)null);
            mockSleeperService.Setup(s => s.GetTrending("add", 24, 10)).ReturnsAsync(new List<TrendingPlayer>
            {
                new() { player_id = "2505", count = 244611 }
            });
            mockSleeperService.Setup(s => s.GetPlayer("2505")).Returns(new SleeperPlayer
            {
                player_id = "2505", full_name = "Jameis Winston", position = "QB", team = "NYG"
            });

            await CreateModule().GetTrending();

            var embed = CaptureEmbed();
            Assert.Contains("1. Jameis Winston (QB - NYG) — 244,611 adds", embed.Description);
            mockDataService.Verify(d => d.GetSleeperSubscription(It.IsAny<ulong>()), Times.Never);
        }

        [Fact]
        public async Task Trending_DropType_IsPassedThrough()
        {
            mockSleeperService.Setup(s => s.GetTrending("drop", 24, 5)).ReturnsAsync(new List<TrendingPlayer>());

            await CreateModule().GetTrending("drop", 5);

            mockSleeperService.Verify(s => s.GetTrending("drop", 24, 5), Times.Once);
        }

        [Fact]
        public async Task Trending_UnrecognisedType_FallsBackToAdd()
        {
            mockSleeperService.Setup(s => s.GetTrending("add", 24, 10)).ReturnsAsync(new List<TrendingPlayer>());

            await CreateModule().GetTrending("sideways");

            mockSleeperService.Verify(s => s.GetTrending("add", 24, 10), Times.Once);
        }

        // --- Scoring inference ---

        [Theory]
        [InlineData(1.0, "PPR")]
        [InlineData(0.5, "Half-PPR")]
        [InlineData(0.0, "Standard")]
        public void DescribeScoring_InfersFormatFromReceptionValue(double rec, string expected)
        {
            var league = new League
            {
                scoring_settings = new Dictionary<string, object> { ["rec"] = rec }
            };

            Assert.Equal(expected, SleeperFantasyModule.DescribeScoring(league));
        }

        [Fact]
        public void DescribeScoring_NoReceptionSetting_IsUnknown()
        {
            Assert.Equal("Unknown", SleeperFantasyModule.DescribeScoring(new League()));
            Assert.Equal("Unknown", SleeperFantasyModule.DescribeScoring(null));
        }

        // --- Error handling ---

        [Fact]
        public async Task Standings_ServiceThrows_RepliesWithTheError()
        {
            GivenSubscription();
            mockSleeperService.Setup(s => s.GetLeagueContext(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Sleeper is down"));

            await CreateModule().GetStandings();

            VerifyTextReply(s => s.Contains("Something went wrong") && s.Contains("Sleeper is down"),
                "an API failure should surface as a message, not an unhandled exception");
        }
    }
}
