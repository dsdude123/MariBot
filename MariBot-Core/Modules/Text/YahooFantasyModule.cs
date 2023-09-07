using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MariBot.Core.Models.Yahoo.FantasySports;
using MariBot.Core.Services;
using Timer = System.Timers.Timer;

namespace MariBot.Core.Modules.Text
{
    [Group("fantasy")]
    public class YahooFantasyModule : ModuleBase<SocketCommandContext>
    {

        private System.Timers.Timer CheckTimer = new Timer { AutoReset = true, Enabled = true, Interval = 900000 };
        private static readonly Dictionary<ulong, string> guildLeagueMapping = new()
        {
            //{ 410597263363276801, "423.l.161379" },
            { 829910467622338580, "423.l.161379" }
        };

        private static readonly Dictionary<ulong, ulong> guildDestinations = new()
        {
            { 829910467622338580, 1012515441685569546}
        };

        private static readonly EmbedFooterBuilder yahooFooter = new EmbedFooterBuilder()
            .WithText("Yahoo Fantasy Sports")
            .WithIconUrl(
                "https://zenprospect-production.s3.amazonaws.com/uploads/pictures/5dcdbfc4248bd70001cf152e/picture");

        private readonly YahooFantasyService yahooFantasyService;
        private readonly DiscordSocketClient discord;

        public YahooFantasyModule(YahooFantasyService yahooFantasyService, DiscordSocketClient discord)
        {
            this.yahooFantasyService = yahooFantasyService;
            this.discord = discord;
            CheckTimer.Elapsed += HandleTimer;
        }

        private async void HandleTimer(object? sender, ElapsedEventArgs e)
        {
            var posts = new Dictionary<EmbedBuilder, ulong>();
            foreach (var guild in guildLeagueMapping.Keys)
            {
                var transactions = await yahooFantasyService.GetTransactions(guildLeagueMapping[guild]);
                foreach (var transaction in transactions.league.transactions)
                {
                    if ((transaction.type.Equals("add/drop") || transaction.type.Equals("trade")) && transaction.status.Equals("successful"))
                    {
                        DateTimeOffset transactionTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(transaction.timestamp));
                        if (transactionTime > DateTimeOffset.Now.AddMinutes(-15))
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
                                    } else if (player.transactionData.type.Equals("drop"))
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

                            } else if (transaction.type.Equals("trade"))
                            {
                                List<Player> traderReceivesPlayers = new List<Player>();
                                List<Player> tradeeReceivesPlayers = new List<Player>();
                                List<Pick> traderReceivesPicks = new List<Pick>();
                                List<Pick> tradeeReceivesPicks = new List<Pick>();

                                foreach (var player in transaction.players)
                                {
                                    if (player.transactionData.destinationTeamName.Equals(transaction.traderTeamName)) 
                                    {
                                        traderReceivesPlayers.Add(player);
                                    }
                                    else if (player.transactionData.destinationTeamName.Equals(transaction.tradeeTeamName))
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

                                eb.WithDescription($"{transaction.traderTeamName} and {transaction.tradeeTeamName} have completed a trade");
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

        [Command("authenticate", RunMode = RunMode.Async)]
        public async Task Authenticate(string code = null)
        {
            if (code == null)
            {
                Context.Channel.SendMessageAsync(yahooFantasyService.GetAuthLink(),
                    messageReference: new MessageReference(Context.Message.Id));
            }
            else
            {
                await yahooFantasyService.Authorize(code);
                Context.Channel.SendMessageAsync("Authenticated to Yahoo Fantasy Sports",
                    messageReference: new MessageReference(Context.Message.Id));
            }
        }

        [Command("raw", RunMode = RunMode.Async)]
        public async Task GetRaw(string url)
        {
            string result = yahooFantasyService.GetRaw(url).Result;

            Context.Channel.SendMessageAsync(result, messageReference: new MessageReference(Context.Message.Id));
        }

        [Command("league", RunMode = RunMode.Async)]
        public async Task GetLeague()
        {
            League result = yahooFantasyService.GetLeague(guildLeagueMapping[Context.Guild.Id]).Result.league;

            var eb = new EmbedBuilder();
            eb.WithFooter(yahooFooter);
            eb.WithTitle(result.name);
            eb.WithUrl(result.url);
            eb.WithColor(Color.Purple);
            eb.AddField("Draft Status", result.draftStatus,true);
            eb.AddField("Number of Teams", result.numTeams, true);
            eb.AddField("Current Week", result.currentWeek, true);
            eb.AddField("Scoring Type", result.scoringType, true);
            eb.AddField("Start Week", result.startWeek, true);
            eb.AddField("End Week", result.endWeek, true);
            eb.AddField("Is Finished", result.isFinished.ToString(), true);
            eb.WithThumbnailUrl(result.logoUrl);

            Context.Channel.SendMessageAsync(embed: eb.Build(),
                messageReference: new MessageReference(Context.Message.Id));
        }

        [Command("standings", RunMode = RunMode.Async)]
        public async Task GetStandings()
        {
            League result = yahooFantasyService.GetStandings(guildLeagueMapping[Context.Guild.Id]).Result.league;

            var eb = new EmbedBuilder();
            eb.WithFooter(yahooFooter);
            eb.WithTitle(result.name);
            eb.WithDescription($"League Standings Week {result.currentWeek}");
            eb.WithUrl(result.url);
            eb.WithColor(Color.Purple);
            eb.WithThumbnailUrl(result.logoUrl);

            Team[] sortedTeams = new Team[result.standings.teams.Count];

            for (int i = 0; i < sortedTeams.Length; i++)
            {
                Team team = result.standings.teams[i];
                if (team.teamStandings.rank == 0)
                {
                    team.teamStandings.rank = (uint)i + 1;
                }
                sortedTeams[team.teamStandings.rank - 1] = team;
            }

            for (int i = 0; i < sortedTeams.Length; i++)
            {
                Team team = sortedTeams[i];
                eb.AddField($"{i + 1}. {team.name}",
                    $"{team.teamStandings.outcomeTotals.wins}-{team.teamStandings.outcomeTotals.losses}");
            }

            Context.Channel.SendMessageAsync(embed: eb.Build(),
                messageReference: new MessageReference(Context.Message.Id));
        }


        [Command("scoreboard", RunMode = RunMode.Async)]
        public async Task GetScoreboard()
        {
            League result = yahooFantasyService.GetScoreboard(guildLeagueMapping[Context.Guild.Id]).Result.league;

            var eb = new EmbedBuilder();
            eb.WithFooter(yahooFooter);
            eb.WithTitle(result.name);
            eb.WithDescription($"League Scoreboard Week {result.currentWeek}");
            eb.WithUrl(result.url);
            eb.WithColor(Color.Purple);
            eb.WithThumbnailUrl(result.logoUrl);

            foreach (var match in result.scoreboard.matchups)
            {
                Team teamOne = match.teams[0];
                Team teamTwo = match.teams[1];

                eb.AddField($"{teamOne.name} vs. {teamTwo.name}",
                    $"Current: {teamOne.teamPoints.total}-{teamTwo.teamPoints.total}    Projected: {teamOne.teamProjectedPoints.total}-{teamTwo.teamProjectedPoints.total}");
            }

            Context.Channel.SendMessageAsync(embed: eb.Build(),
                messageReference: new MessageReference(Context.Message.Id));
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
