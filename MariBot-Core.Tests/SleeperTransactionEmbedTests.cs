using MariBot.Core.Models.Sleeper;
using MariBot.Core.Utils;
using Newtonsoft.Json;
using Xunit;

namespace MariBot.Core.Tests
{
    /// <summary>
    /// Covers the transaction embed builders. The fixtures are trimmed copies of real
    /// /league/{id}/transactions/{round} responses, so the shapes here — adds/drops as
    /// player_id-to-roster_id maps, null settings on trades — are the ones Sleeper
    /// actually sends rather than the ones the docs imply.
    /// </summary>
    public class SleeperTransactionEmbedTests
    {
        private static readonly League League = new()
        {
            league_id = "289646328504385536",
            name = "Sleeper Friends League",
            avatar = "efaefa889ae24046a53265a3c71b8b64"
        };

        private static readonly Dictionary<int, string> RosterNames = new()
        {
            { 2, "Giant Dolphins" },
            { 4, "Team Four" },
            { 6, "Team Six" },
            { 9, "Team Nine" }
        };

        private static readonly Dictionary<string, SleeperPlayer> Catalog = new()
        {
            ["4973"] = new SleeperPlayer
            {
                player_id = "4973", full_name = "Josh Allen", position = "QB", team = "BUF"
            },
            ["4993"] = new SleeperPlayer
            {
                player_id = "4993", full_name = "Dede Westbrook", position = "WR", team = "JAX"
            },
            ["2341"] = new SleeperPlayer
            {
                player_id = "2341", full_name = "Amari Cooper", position = "WR", team = "DAL"
            },
            ["4068"] = new SleeperPlayer
            {
                player_id = "4068", full_name = "Kerryon Johnson", position = "RB", team = "DET"
            },
            ["CLE"] = new SleeperPlayer
            {
                // Team defenses carry no full_name, only first/last.
                player_id = "CLE", first_name = "Cleveland", last_name = "Browns",
                position = "DEF", team = "CLE"
            }
        };

        private static SleeperPlayer? Lookup(string id) => Catalog.TryGetValue(id, out var p) ? p : null;

        private static Transaction Parse(string json) => JsonConvert.DeserializeObject<Transaction>(json)!;

        [Fact]
        public void WaiverTransaction_RendersAddsDropsAndBid()
        {
            var transaction = Parse(@"{
                ""status"": ""complete"",
                ""type"": ""waiver"",
                ""created"": 1539153517833,
                ""settings"": { ""waiver_bid"": 3 },
                ""leg"": 5,
                ""draft_picks"": [],
                ""transaction_id"": ""355639266249850880"",
                ""adds"": { ""4973"": 2 },
                ""drops"": { ""4993"": 2 },
                ""roster_ids"": [2],
                ""status_updated"": 1539155042276
            }");

            var embed = SleeperEmbeds.BuildTransactionEmbed(transaction, League, RosterNames, Lookup).Build();

            Assert.Equal("Giant Dolphins completed a waiver transaction", embed.Description);

            var adds = embed.Fields.Single(f => f.Name == "Adds");
            Assert.Equal("Josh Allen - BUF - QB ($3)", adds.Value);

            var drops = embed.Fields.Single(f => f.Name == "Drops");
            Assert.Equal("Dede Westbrook - JAX - WR", drops.Value);
        }

        [Fact]
        public void FreeAgentTransaction_HasNoBidSuffix()
        {
            var transaction = Parse(@"{
                ""status"": ""complete"",
                ""type"": ""free_agent"",
                ""settings"": null,
                ""draft_picks"": [],
                ""transaction_id"": ""1"",
                ""adds"": { ""4973"": 4 },
                ""drops"": null,
                ""roster_ids"": [4],
                ""status_updated"": 1539155042276
            }");

            var embed = SleeperEmbeds.BuildTransactionEmbed(transaction, League, RosterNames, Lookup).Build();

            Assert.Equal("Team Four completed a free agent transaction", embed.Description);
            Assert.Equal("Josh Allen - BUF - QB", embed.Fields.Single(f => f.Name == "Adds").Value);
            Assert.DoesNotContain(embed.Fields, f => f.Name == "Drops");
        }

        [Fact]
        public void AddDrop_UsesTheRosterFromTheAddsMapNotRosterIds()
        {
            // roster_ids alone does not say who received the player; only the adds map does.
            var transaction = Parse(@"{
                ""status"": ""complete"",
                ""type"": ""waiver"",
                ""settings"": null,
                ""draft_picks"": [],
                ""transaction_id"": ""2"",
                ""adds"": { ""4973"": 6 },
                ""drops"": null,
                ""roster_ids"": [6],
                ""status_updated"": 1
            }");

            var embed = SleeperEmbeds.BuildTransactionEmbed(transaction, League, RosterNames, Lookup).Build();

            Assert.StartsWith("Team Six", embed.Description);
        }

        [Fact]
        public void TwoTeamTrade_RendersOneFieldPerTeamWithWhatTheyReceive()
        {
            var transaction = Parse(@"{
                ""status"": ""complete"",
                ""type"": ""trade"",
                ""settings"": null,
                ""leg"": 1,
                ""draft_picks"": [],
                ""transaction_id"": ""344746950677274624"",
                ""adds"": { ""2341"": 4, ""4068"": 6 },
                ""drops"": { ""2341"": 6, ""4068"": 4 },
                ""roster_ids"": [4, 6],
                ""status_updated"": 1536792967083
            }");

            var embed = SleeperEmbeds.BuildTransactionEmbed(transaction, League, RosterNames, Lookup).Build();

            Assert.Equal(2, embed.Fields.Length);
            Assert.Equal("Amari Cooper - DAL - WR", embed.Fields.Single(f => f.Name == "Team Four receives").Value);
            Assert.Equal("Kerryon Johnson - DET - RB", embed.Fields.Single(f => f.Name == "Team Six receives").Value);
        }

        [Fact]
        public void ThreeTeamTrade_RendersAFieldForEveryParticipant()
        {
            var transaction = Parse(@"{
                ""status"": ""complete"",
                ""type"": ""trade"",
                ""settings"": null,
                ""draft_picks"": [
                    { ""season"": ""2026"", ""round"": 2, ""roster_id"": 4, ""previous_owner_id"": 4, ""owner_id"": 9 }
                ],
                ""transaction_id"": ""3"",
                ""adds"": { ""2341"": 4, ""4068"": 6, ""4973"": 9 },
                ""drops"": { ""2341"": 6, ""4068"": 9, ""4973"": 4 },
                ""roster_ids"": [4, 6, 9],
                ""status_updated"": 1
            }");

            var embed = SleeperEmbeds.BuildTransactionEmbed(transaction, League, RosterNames, Lookup).Build();

            Assert.Equal(3, embed.Fields.Length);
            Assert.Equal("Amari Cooper - DAL - WR", embed.Fields.Single(f => f.Name == "Team Four receives").Value);
            Assert.Equal("Kerryon Johnson - DET - RB", embed.Fields.Single(f => f.Name == "Team Six receives").Value);

            // Roster 9 receives a player and the traded pick.
            var nine = embed.Fields.Single(f => f.Name == "Team Nine receives").Value;
            Assert.Contains("Josh Allen - BUF - QB", nine);
            Assert.Contains("2026 Round 2 pick", nine);
        }

        [Fact]
        public void UnknownPlayerId_FallsBackToTheRawId()
        {
            var transaction = Parse(@"{
                ""status"": ""complete"",
                ""type"": ""free_agent"",
                ""settings"": null,
                ""transaction_id"": ""4"",
                ""adds"": { ""99999"": 2 },
                ""roster_ids"": [2],
                ""status_updated"": 1
            }");

            var embed = SleeperEmbeds.BuildTransactionEmbed(transaction, League, RosterNames, Lookup).Build();

            Assert.Equal("99999", embed.Fields.Single(f => f.Name == "Adds").Value);
        }

        [Fact]
        public void DefenseEntry_UsesFirstAndLastNameSinceFullNameIsNull()
        {
            var transaction = Parse(@"{
                ""status"": ""complete"",
                ""type"": ""free_agent"",
                ""settings"": null,
                ""transaction_id"": ""5"",
                ""adds"": { ""CLE"": 2 },
                ""roster_ids"": [2],
                ""status_updated"": 1
            }");

            var embed = SleeperEmbeds.BuildTransactionEmbed(transaction, League, RosterNames, Lookup).Build();

            Assert.Equal("Cleveland Browns - CLE - DEF", embed.Fields.Single(f => f.Name == "Adds").Value);
        }

        [Fact]
        public void UnknownRosterId_FallsBackToTheRosterNumber()
        {
            var transaction = Parse(@"{
                ""status"": ""complete"",
                ""type"": ""waiver"",
                ""settings"": null,
                ""transaction_id"": ""6"",
                ""adds"": { ""4973"": 77 },
                ""roster_ids"": [77],
                ""status_updated"": 1
            }");

            var embed = SleeperEmbeds.BuildTransactionEmbed(transaction, League, RosterNames, Lookup).Build();

            Assert.StartsWith("Roster 77", embed.Description);
        }

        // --- JoinCapped ---

        [Fact]
        public void JoinCapped_ShortList_JoinsEveryLine()
        {
            var result = SleeperEmbeds.JoinCapped(new[] { "one", "two", "three" });
            Assert.Equal("one\ntwo\nthree", result);
        }

        [Fact]
        public void JoinCapped_EmptyList_ReturnsNone()
        {
            Assert.Equal("None", SleeperEmbeds.JoinCapped(Array.Empty<string>()));
            Assert.Equal("None", SleeperEmbeds.JoinCapped(null));
        }

        [Fact]
        public void JoinCapped_OverLimit_TruncatesAndCountsTheRemainder()
        {
            // 40 lines of 60 characters is 2400, comfortably past Discord's 1024 cap.
            var lines = Enumerable.Range(0, 40).Select(i => new string('x', 59) + i).ToList();

            var result = SleeperEmbeds.JoinCapped(lines);

            Assert.True(result.Length <= SleeperEmbeds.FieldValueLimit,
                $"expected <= 1024 characters, got {result.Length}");
            Assert.Contains("…and", result);
            Assert.EndsWith("more", result);
        }

        [Fact]
        public void JoinCapped_ExactlyAtLimit_KeepsEveryLine()
        {
            var lines = new[] { new string('a', 512), new string('b', 511) };

            var result = SleeperEmbeds.JoinCapped(lines);

            Assert.Equal(1024, result.Length);
            Assert.DoesNotContain("…and", result);
        }
    }
}
