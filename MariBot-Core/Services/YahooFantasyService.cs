using System.Timers;
using Discord;
using Discord.WebSocket;
using MariBot.Core.Models.Yahoo;
using MariBot.Core.Models.Yahoo.FantasySports;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Timer = System.Timers.Timer;

namespace MariBot.Core.Services
{
    public class YahooFantasyService
    {

        private static System.Timers.Timer CheckTimer = new Timer { AutoReset = true, Enabled = true, Interval = 900000 };
        private static int EarliestTransaction = -15;
        public static readonly Dictionary<ulong, string> guildLeagueMapping = new()
        {
            #if DEBUG
            { 410597263363276801, "423.l.161379" },
            #else
            { 829910467622338580, "nfl.l.42250" }
            #endif
        };

        private static readonly Dictionary<ulong, ulong> guildDestinations = new()
        {
            #if DEBUG
            { 410597263363276801, 686064098168799262 },
            #else
            { 829910467622338580, 1012515441685569546}
            #endif
        };

        private static readonly EmbedFooterBuilder yahooFooter = new EmbedFooterBuilder()
            .WithText("Yahoo Fantasy Sports")
            .WithIconUrl(
                "https://zenprospect-production.s3.amazonaws.com/uploads/pictures/5dcdbfc4248bd70001cf152e/picture");


        private static OAuthResponse? authDetails;
        private static DateTime expiresTime = DateTime.MinValue;

        private readonly string clientId;
        private readonly string clientSecret;

        private readonly DiscordSocketClient discord;
        private readonly ILogger<YahooFantasyService> logger;

        public YahooFantasyService(IConfiguration configuration, DiscordSocketClient discord, ILogger<YahooFantasyService> logger)
        {
            this.discord = discord;
            this.logger = logger;
            clientId = configuration["DiscordSettings:YahooFantasyClientId"];
            clientSecret = configuration["DiscordSettings:YahooFantasyClientSecret"];
            CheckTimer.Elapsed += HandleTimer;

            #if DEBUG
            CheckTimer.Enabled = false;
            CheckTimer.Interval = 60000;
            EarliestTransaction = -43200;
            #endif

            try
            {
                authDetails = JsonConvert.DeserializeObject<OAuthResponse>(File.ReadAllText("yahoo-auth.json"));
            }
            catch (Exception ex)
            {
                logger.LogInformation("Did not load auth from disk");
            }
        }

        public string GetAuthLink()
        {
            return
                $"https://api.login.yahoo.com/oauth2/request_auth?client_id={clientId}&redirect_uri=oob&response_type=code&language=en-us";
        }

        public async Task Authorize(string authorizationCode)
        {
            var client = new RestClient();
            client.Authenticator = new HttpBasicAuthenticator(clientId, clientSecret);

            var request = new RestRequest("https://api.login.yahoo.com/oauth2/get_token");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", $"redirect_uri=oob&code={authorizationCode}&grant_type=authorization_code", 
                ParameterType.RequestBody);
            authDetails = await client.PostAsync<OAuthResponse>(request);
            expiresTime = DateTime.Now.AddSeconds(authDetails.expiresIn);
            logger.LogInformation("Yahoo token expires at {}", expiresTime.ToLongTimeString());
            File.WriteAllText("yahoo-auth.json", JsonConvert.SerializeObject(authDetails));
        }

        public async Task<string> GetRaw(string url)
        {
            await RefreshToken();
            var client = new RestClient();
            client.Authenticator = new JwtAuthenticator(authDetails.accessToken);

            var request = new RestRequest(url, Method.GET);
            IRestResponse res = await client.ExecuteAsync(request);

            return res.Content;
        }

        public async Task<FantasyContent> GetLeague(string leagueId)
        {
            await RefreshToken();
            var client = new RestClient();
            client.Authenticator = new JwtAuthenticator(authDetails.accessToken);

            var request = new RestRequest($"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueId}");
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            return await client.GetAsync<FantasyContent>(request);
        }

        public async Task<FantasyContent> GetStandings(string leagueId)
        {
            await RefreshToken();
            var client = new RestClient();
            client.Authenticator = new JwtAuthenticator(authDetails.accessToken);

            var request = new RestRequest($"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueId}/standings");
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            return await client.GetAsync<FantasyContent>(request);
        }

        public async Task<FantasyContent> GetScoreboard(string leagueId)
        {
            await RefreshToken();
            var client = new RestClient();
            client.Authenticator = new JwtAuthenticator(authDetails.accessToken);

            var request = new RestRequest($"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueId}/scoreboard");
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            return await client.GetAsync<FantasyContent>(request);
        }

        public async Task<FantasyContent> GetTransactions(string leagueId)
        {
            await RefreshToken();
            var client = new RestClient();
            client.Authenticator = new JwtAuthenticator(authDetails.accessToken);

            var request = new RestRequest($"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueId}/transactions");
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            return await client.GetAsync<FantasyContent>(request);
        }

        public async Task RefreshToken()
        {
            if (authDetails == null)
            {
                throw new InvalidOperationException("Not authenticated to Yahoo.");
            }

            try
            {
                if (expiresTime < DateTime.Now)
                {
                    var client = new RestClient();
                    client.Authenticator = new HttpBasicAuthenticator(clientId, clientSecret);

                    var request = new RestRequest("https://api.login.yahoo.com/oauth2/get_token");
                    request.AddHeader("content-type", "application/x-www-form-urlencoded");
                    request.AddParameter("application/x-www-form-urlencoded",
                        $"redirect_uri=oob&refresh_token={authDetails.refreshToken}&grant_type=refresh_token",
                        ParameterType.RequestBody);
                    authDetails = await client.PostAsync<OAuthResponse>(request);
                    expiresTime = DateTime.Now.AddSeconds(authDetails.expiresIn);
                    logger.LogInformation("Refreshed token. New expiry time is {}", expiresTime.ToLongTimeString());
                    File.WriteAllText("yahoo-auth.json", JsonConvert.SerializeObject(authDetails));
                }
            }
            finally
            {
                if (authDetails == null || authDetails.accessToken == null || authDetails.refreshToken == null)
                {
                    throw new UnauthorizedAccessException("Failed to authenticate to Yahoo");
                }
            }
        }

        private async void HandleTimer(object? sender, ElapsedEventArgs e)
        {
            try
            {
                await RefreshToken();
                var posts = new Dictionary<EmbedBuilder, ulong>();
                foreach (var guild in guildLeagueMapping.Keys)
                {
                    var transactions = await GetTransactions(guildLeagueMapping[guild]);
                    foreach (var transaction in transactions.league.transactions)
                    {
                        if ((transaction.type.Equals("add/drop") || transaction.type.Equals("trade")) &&
                            transaction.status.Equals("successful"))
                        {
                            DateTimeOffset transactionTime =
                                DateTimeOffset.FromUnixTimeSeconds(long.Parse(transaction.timestamp));
                            if (transactionTime > DateTimeOffset.Now.AddMinutes(EarliestTransaction))
                            {
                                if (transaction.type.Equals("add/drop"))
                                {
                                    List<Player> addPlayers = new List<Player>();
                                    List<Player> dropPlayers = new List<Player>();

                                    foreach (var player in transaction.players)
                                    {
                                        if (player.transactionData.type.Equals("add"))
                                        {
                                            addPlayers.Add(player);
                                        }
                                        else if (player.transactionData.type.Equals("drop"))
                                        {
                                            dropPlayers.Add(player);
                                        }
                                    }

                                    var team = "";
                                    var eb = new EmbedBuilder();
                                    eb.WithFooter(yahooFooter);
                                    eb.WithTitle(transactions.league.name);
                                    eb.WithUrl(transactions.league.url);
                                    eb.WithColor(Color.Purple);


                                    if (addPlayers.Count > 0)
                                    {
                                        string detailText = String.Empty;

                                        foreach (var player in addPlayers)
                                        {
                                            team = player.transactionData.destinationTeamName;
                                            detailText +=
                                                $"{player.name.full} - {player.editorialTeamAbbr} - {player.displayPosition} from {player.transactionData.sourceType.Replace("freeagents", "free agents")}\n";
                                        }

                                        eb.AddField("Adds", detailText);
                                    }

                                    if (dropPlayers.Count > 0)
                                    {
                                        string detailText = String.Empty;

                                        foreach (var player in dropPlayers)
                                        {
                                            if (team.Equals(""))
                                            {
                                                team = player.transactionData.destinationTeamName;
                                            }

                                            detailText +=
                                                $"{player.name.full} - {player.editorialTeamAbbr} - {player.displayPosition} to {player.transactionData.destinationType.Replace("freeagents", "free agents")}\n";
                                        }

                                        eb.AddField("Drops", detailText);
                                    }

                                    eb.WithDescription($"{team} completed Add/Drop transaction");
                                    eb.WithThumbnailUrl(transactions.league.logoUrl);
                                    posts.Add(eb, guild);

                                }
                                else if (transaction.type.Equals("trade"))
                                {
                                    List<Player> traderReceivesPlayers = new List<Player>();
                                    List<Player> tradeeReceivesPlayers = new List<Player>();
                                    List<Pick> traderReceivesPicks = new List<Pick>();
                                    List<Pick> tradeeReceivesPicks = new List<Pick>();

                                    foreach (var player in transaction.players)
                                    {
                                        if (player.transactionData.destinationTeamName.Equals(
                                                transaction.traderTeamName))
                                        {
                                            traderReceivesPlayers.Add(player);
                                        }
                                        else if (player.transactionData.destinationTeamName.Equals(transaction
                                                     .tradeeTeamName))
                                        {
                                            tradeeReceivesPlayers.Add(player);
                                        }
                                    }

                                    foreach (var pick in transaction.picks)
                                    {
                                        if (pick.destinationTeamName.Equals(transaction.traderTeamName))
                                        {
                                            traderReceivesPicks.Add(pick);
                                        }
                                        else if (pick.destinationTeamName.Equals(transaction.tradeeTeamName))
                                        {
                                            tradeeReceivesPicks.Add(pick);
                                        }
                                    }

                                    var eb = new EmbedBuilder();
                                    eb.WithFooter(yahooFooter);
                                    eb.WithTitle(transactions.league.name);
                                    eb.WithUrl(transactions.league.url);
                                    eb.WithColor(Color.Purple);

                                    var traderDetail = "";

                                    foreach (var player in traderReceivesPlayers)
                                    {
                                        traderDetail +=
                                            $"{player.name.full} - {player.editorialTeamAbbr} - {player.displayPosition}\n";
                                    }

                                    foreach (var pick in traderReceivesPicks)
                                    {
                                        traderDetail += $"Round {pick.round}\n";
                                    }


                                    var tradeeDetail = "";

                                    foreach (var player in tradeeReceivesPlayers)
                                    {
                                        tradeeDetail +=
                                            $"{player.name.full} - {player.editorialTeamAbbr} - {player.displayPosition}\n";
                                    }

                                    foreach (var pick in tradeeReceivesPicks)
                                    {
                                        tradeeDetail += $"Round {pick.round}\n";
                                    }

                                    eb.AddField($"{transaction.traderTeamName} receives", traderDetail);
                                    eb.AddField($"{transaction.tradeeTeamName} receives", tradeeDetail);

                                    eb.WithDescription(
                                        $"{transaction.traderTeamName} and {transaction.tradeeTeamName} have completed a trade");
                                    eb.WithThumbnailUrl(transactions.league.logoUrl);
                                    posts.Add(eb, guild);
                                }
                            }
                        }
                    }
                }

                foreach (var post in posts)
                {
                    var guild = FindServer(post.Value);
                    var destination = FindTextChannel(guild, guildDestinations[post.Value]);

                    destination.SendMessageAsync(embed: post.Key.Build());

                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch fantasy transactions");
            }

        }
        private IGuild FindServer(ulong id)
        {
            foreach (IGuild server in discord.Guilds)
            {
                if (server.Id == id)
                    return server;
            }
            return null;
        }

        private ITextChannel FindTextChannel(IGuild server, ulong id)
        {
            foreach (ITextChannel channel in server.GetTextChannelsAsync().Result)
            {
                if (channel.Id == id)
                    return channel;
            }
            return null;
        }
    }
}
