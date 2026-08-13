using MariBot.Core.Models.Sleeper;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Holds the Sleeper NFL player catalog, which every embed needs to turn the bare
    /// player_id values Sleeper returns into names.
    ///
    /// Sleeper asks that /players/nfl be fetched at most once per day. Their docs call
    /// it ~5 MB; it is currently ~14.6 MB across ~12,000 players, which is why the
    /// entries are trimmed to <see cref="SleeperPlayer"/> before being held or written
    /// back to disk, and why the disk copy exists at all — without it every container
    /// restart re-downloads the whole catalog.
    ///
    /// Separate from <see cref="SleeperService"/> so the catalog and its refresh timer
    /// can be tested and mocked on their own.
    /// </summary>
    public class SleeperPlayerCache : IDisposable
    {
        private const string PlayersUrl = "https://api.sleeper.app/v1/players/nfl";
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(24);

        private readonly string cachePath;
        private readonly ILogger<SleeperPlayerCache> logger;
        private readonly SemaphoreSlim refreshLock = new(1, 1);
        private readonly Timer refreshTimer;

        /// <summary>
        /// Replaced wholesale on refresh rather than mutated, so readers never observe
        /// a half-populated catalog and need no lock.
        /// </summary>
        private volatile Dictionary<string, SleeperPlayer> players = new();

        protected SleeperPlayerCache() { }

        public SleeperPlayerCache(IConfiguration configuration, ILogger<SleeperPlayerCache> logger)
        {
            this.logger = logger;
            cachePath = configuration.GetValue<string>("DiscordSettings:SleeperCachePath") ?? "sleeper-players.json";

            var cacheDir = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(cacheDir))
                Directory.CreateDirectory(cacheDir);

            // Escape hatch for running against a pre-seeded cache with no network — and
            // what keeps the tests from pulling 14 MB down on every construction.
            var autoRefresh = configuration.GetValue<bool?>("DiscordSettings:SleeperCacheAutoRefresh") ?? true;

            var loaded = TryLoadFromDisk();

            if (!loaded && autoRefresh)
            {
                // Fire and forget: a cold catalog degrades embeds to raw ids rather
                // than failing them, so nothing needs to block on this completing.
                _ = Task.Run(RefreshAsync);
            }

            refreshTimer = new Timer
            {
                AutoReset = true,
                Enabled = autoRefresh,
                Interval = RefreshInterval.TotalMilliseconds
            };
            refreshTimer.Elapsed += async (_, _) => await RefreshAsync();
        }

        /// <summary>
        /// Number of players currently cached. Zero means the catalog is still cold.
        /// </summary>
        public virtual int Count => players.Count;

        public virtual bool TryGetPlayer(string id, out SleeperPlayer? player)
        {
            player = null;
            return !string.IsNullOrEmpty(id) && players.TryGetValue(id, out player);
        }

        /// <summary>
        /// Catalog entry for a player id, or null when the catalog is cold or the id is
        /// unknown. Embed builders take this as a delegate so they stay free of any
        /// dependency on the cache itself.
        /// </summary>
        public virtual SleeperPlayer? GetPlayer(string id)
        {
            return TryGetPlayer(id, out var player) ? player : null;
        }

        /// <summary>
        /// Display name for a player id, falling back to the id itself when the catalog
        /// is cold or the id is unknown. Sleeper puts team abbreviations ("CLE") in
        /// starters for defense slots, so a miss here is normal and must never throw.
        /// </summary>
        public virtual string GetPlayerName(string id)
        {
            return GetPlayer(id)?.DisplayName ?? id;
        }

        /// <summary>
        /// Refetches the catalog from Sleeper and writes the trimmed copy to disk.
        /// Guarded so a timer tick and a cold-start load cannot race.
        /// </summary>
        public virtual async Task RefreshAsync()
        {
            if (!await refreshLock.WaitAsync(TimeSpan.Zero))
            {
                logger.LogDebug("Sleeper player catalog refresh already in progress");
                return;
            }

            try
            {
                logger.LogInformation("Fetching Sleeper player catalog");

                // Deliberately not RestSharp, which is the idiom everywhere else in this
                // codebase: RestSharp 106 buffers the entire body into a string before
                // deserializing, which for a 14.6 MB dictionary means holding the raw
                // JSON, the string, and the object graph at once. Streaming avoids that.
                using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                await using var stream = await http.GetStreamAsync(PlayersUrl);
                using var reader = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(reader);

                var serializer = JsonSerializer.CreateDefault();
                var fetched = serializer.Deserialize<Dictionary<string, SleeperPlayer>>(jsonReader);

                if (fetched == null || fetched.Count == 0)
                {
                    logger.LogWarning("Sleeper player catalog came back empty, keeping existing copy");
                    return;
                }

                players = fetched;
                logger.LogInformation("Loaded {Count} Sleeper players", fetched.Count);
                SaveToDisk(fetched);
            }
            catch (Exception ex)
            {
                // A failed refresh leaves the previous catalog in place; embeds keep working.
                logger.LogError(ex, "Failed to refresh Sleeper player catalog");
            }
            finally
            {
                refreshLock.Release();
            }
        }

        /// <summary>
        /// Loads the trimmed catalog written by a previous run, if it is still fresh
        /// enough that Sleeper would not want us refetching.
        /// </summary>
        private bool TryLoadFromDisk()
        {
            try
            {
                if (!File.Exists(cachePath))
                    return false;

                var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath);
                if (age > RefreshInterval)
                {
                    logger.LogInformation("Sleeper player cache is {Hours:F1}h old, refetching", age.TotalHours);
                    return false;
                }

                var loaded = JsonConvert.DeserializeObject<Dictionary<string, SleeperPlayer>>(
                    File.ReadAllText(cachePath));

                if (loaded == null || loaded.Count == 0)
                    return false;

                players = loaded;
                logger.LogInformation("Loaded {Count} Sleeper players from {Path}", loaded.Count, cachePath);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not load Sleeper player cache from {Path}", cachePath);
                return false;
            }
        }

        private void SaveToDisk(Dictionary<string, SleeperPlayer> toSave)
        {
            try
            {
                File.WriteAllText(cachePath, JsonConvert.SerializeObject(toSave));
            }
            catch (Exception ex)
            {
                // Non-fatal: the in-memory catalog is already good for this process.
                logger.LogWarning(ex, "Could not write Sleeper player cache to {Path}", cachePath);
            }
        }

        public void Dispose()
        {
            refreshTimer?.Dispose();
            refreshLock?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
