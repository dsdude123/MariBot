using System.Collections.Concurrent;
using System.Net;
using System.Timers;
using Discord.WebSocket;
using MariBot.Core.Models;
using MariBot.Core.Models.Sleeper;
using MariBot.Core.Utils;
using Newtonsoft.Json;
using RestSharp;
using Timer = System.Timers.Timer;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Raised when Sleeper answers with a non-success status, carrying the code so
    /// callers can tell a bad league id (404) from rate limiting or an outage.
    /// </summary>
    public class SleeperApiException : Exception
    {
        public SleeperApiException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }

        public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    }

    /// <summary>
    /// Read-only client for the Sleeper fantasy API, plus the background poller that
    /// announces league transactions to subscribed guilds.
    ///
    /// Sleeper's API needs no key and no OAuth, so unlike
    /// <see cref="YahooFantasyService"/> there is no token cache to keep alive. What it
    /// does not do is resolve names: rosters and transactions come back as bare
    /// roster_id and player_id values, so this service owns roster-to-manager
    /// resolution and leans on <see cref="SleeperPlayerCache"/> for players.
    /// </summary>
    public class SleeperService
    {
        private const string BaseUrl = "https://api.sleeper.app/v1";

        /// <summary>
        /// Matches the Yahoo poller's cadence. Well inside Sleeper's 1000 requests per
        /// minute ceiling for any plausible number of subscribed guilds.
        /// </summary>
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Transactions older than this are never announced, so a guild subscribing
        /// mid-week does not get the whole week dumped into its channel at once.
        /// </summary>
        private static readonly TimeSpan MaxTransactionAge = TimeSpan.FromHours(24);

        /// <summary>
        /// Rosters and users are needed by nearly every command and every poll. A short
        /// TTL collapses the repeated calls a single command would otherwise make
        /// without letting a standings table go visibly stale.
        /// </summary>
        private static readonly TimeSpan ShortCacheTtl = TimeSpan.FromSeconds(60);

        private readonly ConcurrentDictionary<string, (DateTime fetched, object? value)> shortCache = new();

        private readonly DiscordSocketClient discord;
        private readonly DataService dataService;
        private readonly SleeperPlayerCache playerCache;
        private readonly ILogger<SleeperService> logger;
        private readonly Timer pollTimer;

        protected SleeperService() { }

        public SleeperService(IConfiguration configuration, DiscordSocketClient discord, DataService dataService,
            SleeperPlayerCache playerCache, ILogger<SleeperService> logger)
        {
            this.discord = discord;
            this.dataService = dataService;
            this.playerCache = playerCache;
            this.logger = logger;

            pollTimer = new Timer
            {
                AutoReset = true,
                Enabled = true,
                Interval = PollInterval.TotalMilliseconds
            };
            pollTimer.Elapsed += HandlePollTimer;

            logger.LogInformation("Sleeper transaction poller started ({Minutes} minute interval)",
                PollInterval.TotalMinutes);
        }

        // --- HTTP ---

        /// <summary>
        /// Issues a GET against the Sleeper API.
        /// </summary>
        /// <remarks>
        /// RestSharp handles transport, matching the idiom in <see cref="FluxService"/>,
        /// but deserialization goes through Newtonsoft rather than RestSharp 106's own
        /// deserializer: the Sleeper models lean on nested objects and
        /// <c>Dictionary&lt;string, int&gt;</c> maps that RestSharp's built-in JSON
        /// handling does not reliably reconstruct.
        /// </remarks>
        private async Task<T> Get<T>(string path)
        {
            var client = new RestClient();
            var request = new RestRequest($"{BaseUrl}{path}", Method.GET);
            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new SleeperApiException(response.StatusCode,
                    $"Sleeper returned {(int)response.StatusCode} for {path}");
            }

            // Sleeper answers 200 with a bare "null" body for an unknown league rather
            // than 404, so an empty payload is treated the same as not found.
            if (string.IsNullOrWhiteSpace(response.Content) || response.Content == "null")
            {
                throw new SleeperApiException(HttpStatusCode.NotFound, $"Sleeper returned no data for {path}");
            }

            return JsonConvert.DeserializeObject<T>(response.Content)!;
        }

        private async Task<T> GetCached<T>(string key, Func<Task<T>> fetch)
        {
            if (shortCache.TryGetValue(key, out var entry) && DateTime.UtcNow - entry.fetched < ShortCacheTtl)
                return (T)entry.value!;

            var value = await fetch();
            shortCache[key] = (DateTime.UtcNow, value);
            return value;
        }

        // --- Endpoints ---

        public virtual Task<NflState> GetNflState() => Get<NflState>("/state/nfl");

        public virtual Task<League> GetLeague(string leagueId) => Get<League>($"/league/{leagueId}");

        public virtual Task<List<Roster>> GetRosters(string leagueId) =>
            GetCached($"rosters:{leagueId}", () => Get<List<Roster>>($"/league/{leagueId}/rosters"));

        public virtual Task<List<LeagueUser>> GetLeagueUsers(string leagueId) =>
            GetCached($"users:{leagueId}", () => Get<List<LeagueUser>>($"/league/{leagueId}/users"));

        public virtual Task<List<Matchup>> GetMatchups(string leagueId, int week) =>
            Get<List<Matchup>>($"/league/{leagueId}/matchups/{week}");

        public virtual Task<List<Transaction>> GetTransactions(string leagueId, int round) =>
            Get<List<Transaction>>($"/league/{leagueId}/transactions/{round}");

        public virtual Task<List<BracketMatchup>> GetWinnersBracket(string leagueId) =>
            Get<List<BracketMatchup>>($"/league/{leagueId}/winners_bracket");

        public virtual Task<List<BracketMatchup>> GetLosersBracket(string leagueId) =>
            Get<List<BracketMatchup>>($"/league/{leagueId}/losers_bracket");

        public virtual Task<List<TrendingPlayer>> GetTrending(string type, int lookbackHours, int limit) =>
            Get<List<TrendingPlayer>>($"/players/nfl/trending/{type}?lookback_hours={lookbackHours}&limit={limit}");

        public virtual SleeperPlayer? GetPlayer(string playerId) => playerCache?.GetPlayer(playerId);

        // --- League context ---

        /// <summary>
        /// The league plus everything needed to render a name for a roster. Every
        /// command and the poller starts by building one of these, so roster-to-manager
        /// resolution lives in exactly one place.
        /// </summary>
        public record LeagueContext(
            League League,
            List<Roster> Rosters,
            List<LeagueUser> Users,
            Dictionary<int, string> RosterIdToTeamName);

        public virtual async Task<LeagueContext> GetLeagueContext(string leagueId)
        {
            var league = await GetLeague(leagueId);
            var rosters = await GetRosters(leagueId) ?? new List<Roster>();
            var users = await GetLeagueUsers(leagueId) ?? new List<LeagueUser>();

            return new LeagueContext(league, rosters, users, BuildRosterNameMap(rosters, users));
        }

        /// <summary>
        /// Resolves each roster to a display name, preferring the manager's chosen team
        /// name, then their display name, and finally the bare roster number — a roster
        /// can be orphaned with no owner, so the last fallback is not hypothetical.
        /// </summary>
        public static Dictionary<int, string> BuildRosterNameMap(List<Roster>? rosters, List<LeagueUser>? users)
        {
            var usersById = (users ?? new List<LeagueUser>())
                .Where(u => u?.user_id != null)
                .GroupBy(u => u.user_id)
                .ToDictionary(g => g.Key, g => g.First());

            var map = new Dictionary<int, string>();

            foreach (var roster in rosters ?? new List<Roster>())
            {
                string? name = null;

                if (roster.owner_id != null && usersById.TryGetValue(roster.owner_id, out var user))
                {
                    name = !string.IsNullOrWhiteSpace(user.metadata?.team_name)
                        ? user.metadata.team_name
                        : user.display_name;
                }

                map[roster.roster_id] = string.IsNullOrWhiteSpace(name) ? $"Roster {roster.roster_id}" : name;
            }

            return map;
        }

        // --- Poller ---

        private async void HandlePollTimer(object? sender, ElapsedEventArgs e)
        {
            // The timer's event signature forces async void here; everything inside is
            // awaited and the whole body is guarded so a throw cannot reach the timer.
            try
            {
                await PollTransactions();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to poll Sleeper transactions");
            }
        }

        /// <summary>
        /// One poll across every subscribed guild.
        /// </summary>
        public virtual async Task PollTransactions()
        {
            var subscriptions = dataService.GetAllSleeperSubscriptions()?.ToList();
            if (subscriptions == null || subscriptions.Count == 0)
                return;

            NflState state;
            try
            {
                state = await GetNflState();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not read NFL state, skipping Sleeper poll");
                return;
            }

            // leg is 0 through the preseason, and a round of 0 is not a valid
            // transactions URL.
            var round = Math.Max(1, state.leg);

            foreach (var subscription in subscriptions)
            {
                try
                {
                    await PollSubscription(subscription, state, round);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to poll Sleeper league {LeagueId} for guild {GuildId}",
                        subscription.LeagueId, subscription.GuildId);
                }
            }
        }

        private async Task PollSubscription(SleeperSubscription subscription, NflState state, int round)
        {
            var context = await GetLeagueContext(subscription.LeagueId);

            // Nothing transacts before a league has drafted, so skip the calls entirely.
            if (state.season_type == "pre" &&
                (context.League.status == "pre_draft" || context.League.status == "drafting"))
            {
                logger.LogDebug("Sleeper league {LeagueId} has not drafted yet, skipping poll", subscription.LeagueId);
                return;
            }

            var channel = discord.GetGuild(subscription.GuildId)?.GetTextChannel(subscription.AnnouncementChannelId);
            if (channel == null)
            {
                logger.LogWarning("Sleeper announcement channel {ChannelId} in guild {GuildId} is unavailable",
                    subscription.AnnouncementChannelId, subscription.GuildId);
                return;
            }

            // A transaction completed just after a week rollover is filed under the
            // previous round, so both are fetched — a single-round window would drop it.
            var transactions = new List<Transaction>();
            transactions.AddRange(await GetTransactions(subscription.LeagueId, round) ?? new List<Transaction>());
            if (round > 1)
                transactions.AddRange(await GetTransactions(subscription.LeagueId, round - 1) ?? new List<Transaction>());

            subscription.PostedTransactionIds ??= new HashSet<string>();

            var cutoff = DateTimeOffset.UtcNow.Subtract(MaxTransactionAge).ToUnixTimeMilliseconds();

            var pending = transactions
                .Where(t => t.status == "complete")
                .Where(t => t.type == "trade" || t.type == "waiver" || t.type == "free_agent")
                .Where(t => !subscription.PostedTransactionIds.Contains(t.transaction_id))
                .Where(t => t.status_updated >= cutoff)
                .OrderBy(t => t.status_updated)
                .ToList();

            foreach (var transaction in pending)
            {
                var embed = SleeperEmbeds.BuildTransactionEmbed(
                    transaction, context.League, context.RosterIdToTeamName, playerCache.GetPlayer);

                await channel.SendMessageAsync(embed: embed.Build());

                // Persisted after each post rather than at the end of the tick, so a
                // crash part-way through cannot replay what was already announced.
                subscription.PostedTransactionIds.Add(transaction.transaction_id);
                subscription.LastTransactionTimestamp =
                    Math.Max(subscription.LastTransactionTimestamp, transaction.status_updated);
                dataService.UpdateSleeperSubscription(subscription);

                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            // Keep only ids still visible in the rounds being polled, so the document
            // does not accumulate every transaction of the season.
            var live = transactions.Select(t => t.transaction_id).ToHashSet();
            var trimmed = subscription.PostedTransactionIds.Where(live.Contains).ToHashSet();

            if (trimmed.Count != subscription.PostedTransactionIds.Count)
            {
                subscription.PostedTransactionIds = trimmed;
                dataService.UpdateSleeperSubscription(subscription);
            }
        }
    }
}
