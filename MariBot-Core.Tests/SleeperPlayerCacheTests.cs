using MariBot.Core.Models.Sleeper;
using MariBot.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace MariBot.Core.Tests
{
    /// <summary>
    /// Exercises the disk half of the player cache. The network fetch is not covered —
    /// there is no HTTP mocking in this repo, and the catalog is 14 MB — so the seam
    /// under test is "a cache file written by a previous run is reused".
    /// </summary>
    public class SleeperPlayerCacheTests : IDisposable
    {
        private readonly string cachePath = Path.Combine(Environment.CurrentDirectory,
            $"sleeper-players-test-{Guid.NewGuid():N}.json");

        private SleeperPlayerCache CreateCache()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DiscordSettings:SleeperCachePath"] = cachePath,
                    // Keeps these tests off the network; the 14 MB fetch is not under test.
                    ["DiscordSettings:SleeperCacheAutoRefresh"] = "false"
                })
                .Build();

            return new SleeperPlayerCache(configuration, NullLogger<SleeperPlayerCache>.Instance);
        }

        private void WriteCatalog(Dictionary<string, SleeperPlayer> catalog)
        {
            File.WriteAllText(cachePath, JsonConvert.SerializeObject(catalog));
        }

        private static Dictionary<string, SleeperPlayer> SampleCatalog() => new()
        {
            ["4973"] = new SleeperPlayer
            {
                player_id = "4973", full_name = "Josh Allen", first_name = "Josh", last_name = "Allen",
                position = "QB", team = "BUF", number = 17, injury_status = "Questionable",
                fantasy_positions = new List<string> { "QB" }
            },
            ["CLE"] = new SleeperPlayer
            {
                player_id = "CLE", first_name = "Cleveland", last_name = "Browns",
                position = "DEF", team = "CLE", fantasy_positions = new List<string> { "DEF" }
            }
        };

        public void Dispose()
        {
            if (File.Exists(cachePath))
                File.Delete(cachePath);
        }

        [Fact]
        public void FreshCacheFile_IsLoadedFromDisk()
        {
            WriteCatalog(SampleCatalog());

            var cache = CreateCache();

            Assert.Equal(2, cache.Count);
            Assert.Equal("Josh Allen", cache.GetPlayerName("4973"));
        }

        [Fact]
        public void CacheFile_RoundTripsEveryFieldTheEmbedsUse()
        {
            WriteCatalog(SampleCatalog());

            var cache = CreateCache();

            Assert.True(cache.TryGetPlayer("4973", out var player));
            Assert.NotNull(player);
            Assert.Equal("QB", player!.position);
            Assert.Equal("BUF", player.team);
            Assert.Equal("Questionable", player.injury_status);
            Assert.Equal(17, player.number);
            Assert.Equal(new[] { "QB" }, player.fantasy_positions);
        }

        [Fact]
        public void DefenseEntry_DisplayNameFallsBackToFirstAndLast()
        {
            WriteCatalog(SampleCatalog());

            var cache = CreateCache();

            // full_name is null for team defenses in the real catalog.
            Assert.Equal("Cleveland Browns", cache.GetPlayerName("CLE"));
        }

        [Fact]
        public void UnknownId_FallsBackToTheRawId()
        {
            WriteCatalog(SampleCatalog());

            var cache = CreateCache();

            Assert.Equal("99999", cache.GetPlayerName("99999"));
            Assert.Null(cache.GetPlayer("99999"));
            Assert.False(cache.TryGetPlayer("99999", out _));
        }

        [Fact]
        public void StaleCacheFile_IsNotUsed()
        {
            WriteCatalog(SampleCatalog());
            // Sleeper asks for at most one fetch per day, so anything older is refetched.
            File.SetLastWriteTimeUtc(cachePath, DateTime.UtcNow.AddHours(-25));

            var cache = CreateCache();

            // The background refetch is not awaited here; what matters is that the stale
            // file was rejected rather than served.
            Assert.Equal("4973", cache.GetPlayerName("4973"));
        }

        [Fact]
        public void CorruptCacheFile_DoesNotThrow()
        {
            File.WriteAllText(cachePath, "{ this is not json");

            var cache = CreateCache();

            Assert.Equal("4973", cache.GetPlayerName("4973"));
        }

        [Fact]
        public void ColdCache_ResolvesNamesToRawIdsRatherThanFailing()
        {
            // No file on disk at all.
            var cache = CreateCache();

            Assert.Equal("4973", cache.GetPlayerName("4973"));
        }
    }
}
