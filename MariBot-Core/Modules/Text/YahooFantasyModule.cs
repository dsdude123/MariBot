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
        private static readonly EmbedFooterBuilder yahooFooter = new EmbedFooterBuilder()
            .WithText("Yahoo Fantasy Sports")
            .WithIconUrl(
                "https://zenprospect-production.s3.amazonaws.com/uploads/pictures/5dcdbfc4248bd70001cf152e/picture");

        private readonly YahooFantasyService yahooFantasyService;

        public YahooFantasyModule(YahooFantasyService yahooFantasyService)
        {
            this.yahooFantasyService = yahooFantasyService;
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

            await Context.Channel.SendMessageAsync(result, messageReference: new MessageReference(Context.Message.Id));
        }

        [Command("league", RunMode = RunMode.Async)]
        public async Task GetLeague()
        {
            FantasyContent result = await yahooFantasyService.GetLeague(YahooFantasyService.guildLeagueMapping[Context.Guild.Id]);

            var eb = new EmbedBuilder();
            eb.WithFooter(yahooFooter);
            eb.WithTitle(result.league.name);
            eb.WithUrl(result.league.url);
            eb.WithColor(Color.Purple);
            eb.AddField("Draft Status", result.league.draftStatus,true);
            eb.AddField("Number of Teams", result.league.numTeams, true);
            eb.AddField("Current Week", result.league.currentWeek, true);
            eb.AddField("Scoring Type", result.league.scoringType, true);
            eb.AddField("Start Week", result.league.startWeek, true);
            eb.AddField("End Week", result.league.endWeek, true);
            eb.AddField("Is Finished", result.league.isFinished.ToString(), true);
            eb.WithThumbnailUrl(result.league.logoUrl);

            await Context.Channel.SendMessageAsync(embed: eb.Build(),
                messageReference: new MessageReference(Context.Message.Id));
        }

        [Command("standings", RunMode = RunMode.Async)]
        public async Task GetStandings()
        {
            FantasyContent result = await yahooFantasyService.GetStandings(YahooFantasyService.guildLeagueMapping[Context.Guild.Id]);

            var eb = new EmbedBuilder();
            eb.WithFooter(yahooFooter);
            eb.WithTitle(result.league.name);
            eb.WithDescription($"League Standings Week {result.league.currentWeek}");
            eb.WithUrl(result.league.url);
            eb.WithColor(Color.Purple);
            eb.WithThumbnailUrl(result.league.logoUrl);

            Team[] sortedTeams = new Team[result.league.standings.teams.Count];

            for (int i = 0; i < sortedTeams.Length; i++)
            {
                Team team = result.league.standings.teams[i];
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

            await Context.Channel.SendMessageAsync(embed: eb.Build(),
                messageReference: new MessageReference(Context.Message.Id));
        }


        [Command("scoreboard", RunMode = RunMode.Async)]
        public async Task GetScoreboard()
        {
            FantasyContent result = await yahooFantasyService.GetScoreboard(YahooFantasyService.guildLeagueMapping[Context.Guild.Id]);

            var eb = new EmbedBuilder();
            eb.WithFooter(yahooFooter);
            eb.WithTitle(result.league.name);
            eb.WithDescription($"League Scoreboard Week {result.league.currentWeek}");
            eb.WithUrl(result.league.url);
            eb.WithColor(Color.Purple);
            eb.WithThumbnailUrl(result.league.logoUrl);

            foreach (var match in result.league.scoreboard.matchups)
            {
                Team teamOne = match.teams[0];
                Team teamTwo = match.teams[1];

                eb.AddField($"{teamOne.name} vs. {teamTwo.name}",
                    $"Current: {teamOne.teamPoints.total}-{teamTwo.teamPoints.total}    Projected: {teamOne.teamProjectedPoints.total}-{teamTwo.teamProjectedPoints.total}");
            }

            await Context.Channel.SendMessageAsync(embed: eb.Build(),
                messageReference: new MessageReference(Context.Message.Id));
        }
    }
}
