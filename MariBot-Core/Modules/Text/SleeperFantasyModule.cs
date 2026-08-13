using Discord;
using Discord.Commands;
using MariBot.Core.Models;
using MariBot.Core.Models.Sleeper;
using MariBot.Core.Services;
using MariBot.Core.Utils;

namespace MariBot.Core.Modules.Text
{
    /// <summary>
    /// Sleeper fantasy football commands. Which league a guild is looking at comes from
    /// a runtime subscription rather than a compiled-in mapping, so this group works for
    /// any guild that runs <c>z fantasy subscribe</c>.
    /// </summary>
    [Group("fantasy")]
    public class SleeperFantasyModule : ModuleBase<ICommandContext>
    {
        private const string NoSubscriptionMessage =
            "No Sleeper league is configured for this server. A moderator can set one with `z fantasy subscribe <leagueId>`.";

        private readonly DataService dataService;
        private readonly SleeperService sleeperService;
        private readonly ILogger<SleeperFantasyModule> logger;

        public SleeperFantasyModule(DataService dataService, SleeperService sleeperService,
            ILogger<SleeperFantasyModule> logger)
        {
            this.dataService = dataService;
            this.sleeperService = sleeperService;
            this.logger = logger;
        }

        private Task<IUserMessage> Reply(string text) =>
            Context.Channel.SendMessageAsync(text, messageReference: new MessageReference(Context.Message.Id));

        private Task<IUserMessage> Reply(EmbedBuilder embed) =>
            Context.Channel.SendMessageAsync(embed: embed.Build(),
                messageReference: new MessageReference(Context.Message.Id));

        /// <summary>
        /// Resolves the guild's subscription, or replies and returns null. Guards against
        /// being run in a DM as well as against an unconfigured guild.
        /// </summary>
        private async Task<SleeperSubscription?> RequireSubscription()
        {
            if (Context.Guild == null)
            {
                await Reply("Fantasy commands only work in a server, not in DMs.");
                return null;
            }

            var subscription = dataService.GetSleeperSubscription(Context.Guild.Id);
            if (subscription == null)
            {
                await Reply(NoSubscriptionMessage);
                return null;
            }

            return subscription;
        }

        [RequireUserPermission(GuildPermission.ManageWebhooks)]
        [Command("subscribe", RunMode = RunMode.Async)]
        public async Task Subscribe(string leagueId)
        {
            try
            {
                if (Context.Guild == null)
                {
                    await Reply("Fantasy commands only work in a server, not in DMs.");
                    return;
                }

                League league;
                try
                {
                    league = await sleeperService.GetLeague(leagueId);
                }
                catch (SleeperApiException ex) when (ex.IsNotFound)
                {
                    await Reply("No Sleeper league with that ID.");
                    return;
                }

                var subscription = new SleeperSubscription
                {
                    GuildId = Context.Guild.Id,
                    LeagueId = leagueId,
                    AnnouncementChannelId = Context.Channel.Id,
                    PostedTransactionIds = new HashSet<string>()
                };

                var message = dataService.UpdateSleeperSubscription(subscription)
                    ? $"Subscribed to **{league.name}**. Transactions will be announced in this channel."
                    : "Failed to save the subscription.";

                await Reply(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to subscribe to Sleeper league {LeagueId}", leagueId);
                await Reply($"Something went wrong. {ex.Message}");
            }
        }

        [RequireUserPermission(GuildPermission.ManageWebhooks)]
        [Command("unsubscribe", RunMode = RunMode.Async)]
        public async Task Unsubscribe()
        {
            try
            {
                if (await RequireSubscription() == null)
                    return;

                var message = dataService.DeleteSleeperSubscription(Context.Guild.Id)
                    ? "Unsubscribed. Transactions will no longer be announced."
                    : "Failed to remove the subscription.";

                await Reply(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to unsubscribe from Sleeper league");
                await Reply($"Something went wrong. {ex.Message}");
            }
        }

        [Command("league", RunMode = RunMode.Async)]
        public async Task GetLeague()
        {
            try
            {
                var subscription = await RequireSubscription();
                if (subscription == null)
                    return;

                var context = await sleeperService.GetLeagueContext(subscription.LeagueId);
                var league = context.League;
                var state = await sleeperService.GetNflState();

                var eb = SleeperEmbeds.BaseEmbed(league);
                eb.AddField("Status", league.status ?? "unknown", true);
                eb.AddField("Season", $"{league.season} ({league.season_type})", true);
                eb.AddField("Teams", league.total_rosters.ToString(), true);
                eb.AddField("Current Week", state.display_week.ToString(), true);
                eb.AddField("Scoring", DescribeScoring(league), true);

                if (league.roster_positions != null && league.roster_positions.Count > 0)
                    eb.AddField("Roster Positions", string.Join(", ", league.roster_positions));

                await Reply(eb);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch Sleeper league");
                await Reply($"Something went wrong. {ex.Message}");
            }
        }

        /// <summary>
        /// Sleeper does not label a league's scoring format, so it is inferred from what
        /// a reception is worth.
        /// </summary>
        public static string DescribeScoring(League? league)
        {
            if (league?.scoring_settings == null || !league.scoring_settings.TryGetValue("rec", out var raw))
                return "Unknown";

            double rec;
            try
            {
                rec = Convert.ToDouble(raw);
            }
            catch (Exception)
            {
                return "Unknown";
            }

            if (rec >= 1.0) return "PPR";
            if (rec > 0) return "Half-PPR";
            return "Standard";
        }

        [Command("standings", RunMode = RunMode.Async)]
        public async Task GetStandings()
        {
            try
            {
                var subscription = await RequireSubscription();
                if (subscription == null)
                    return;

                var context = await sleeperService.GetLeagueContext(subscription.LeagueId);

                var eb = SleeperEmbeds.BaseEmbed(context.League);
                eb.WithDescription("League Standings");

                var ranked = SortStandings(context.Rosters);

                for (var i = 0; i < ranked.Count; i++)
                {
                    var roster = ranked[i];
                    var settings = roster.settings;
                    var name = context.RosterIdToTeamName.TryGetValue(roster.roster_id, out var teamName)
                        ? teamName
                        : $"Roster {roster.roster_id}";

                    eb.AddField($"{i + 1}. {name}",
                        $"{settings.wins}-{settings.losses}-{settings.ties} · {settings.PointsFor:F2} PF · {settings.PointsAgainst:F2} PA");
                }

                await Reply(eb);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch Sleeper standings");
                await Reply($"Something went wrong. {ex.Message}");
            }
        }

        /// <summary>
        /// Wins first, then fewest losses, then points scored as the tiebreak.
        /// </summary>
        public static List<Roster> SortStandings(List<Roster> rosters)
        {
            return (rosters ?? new List<Roster>())
                .Where(r => r?.settings != null)
                .OrderByDescending(r => r.settings.wins)
                .ThenBy(r => r.settings.losses)
                .ThenByDescending(r => r.settings.PointsFor)
                .ToList();
        }

        [Command("scoreboard", RunMode = RunMode.Async)]
        public async Task GetScoreboard(int week = 0)
        {
            try
            {
                var subscription = await RequireSubscription();
                if (subscription == null)
                    return;

                if (week <= 0)
                    week = (await sleeperService.GetNflState()).display_week;

                var context = await sleeperService.GetLeagueContext(subscription.LeagueId);
                var matchups = await sleeperService.GetMatchups(subscription.LeagueId, week);

                var eb = SleeperEmbeds.BaseEmbed(context.League);
                eb.WithDescription($"League Scoreboard Week {week}");

                var pairings = PairMatchups(matchups);

                if (pairings.Count == 0)
                {
                    eb.AddField("No matchups", $"Nothing scheduled for week {week}.");
                }
                else
                {
                    foreach (var (a, b) in pairings)
                    {
                        var nameA = context.RosterIdToTeamName.TryGetValue(a.roster_id, out var na)
                            ? na
                            : $"Roster {a.roster_id}";

                        if (b == null)
                        {
                            // No opponent this week — a bye, or an unpaired roster.
                            eb.AddField($"{nameA} (bye)", $"{a.points:F2}");
                            continue;
                        }

                        var nameB = context.RosterIdToTeamName.TryGetValue(b.roster_id, out var nb)
                            ? nb
                            : $"Roster {b.roster_id}";

                        // Sleeper's public API exposes no projections, so unlike the Yahoo
                        // scoreboard this shows actual points only.
                        eb.AddField($"{nameA} vs. {nameB}", $"{a.points:F2} - {b.points:F2}");
                    }
                }

                await Reply(eb);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch Sleeper scoreboard");
                await Reply($"Something went wrong. {ex.Message}");
            }
        }

        /// <summary>
        /// Sleeper returns one entry per roster and leaves the pairing to the caller:
        /// entries sharing a matchup_id are opponents, and a null matchup_id means the
        /// roster has none that week.
        /// </summary>
        public static List<(Matchup, Matchup?)> PairMatchups(List<Matchup>? matchups)
        {
            var result = new List<(Matchup, Matchup?)>();
            if (matchups == null)
                return result;

            foreach (var group in matchups.Where(m => m?.matchup_id != null)
                         .GroupBy(m => m.matchup_id!.Value)
                         .OrderBy(g => g.Key))
            {
                var teams = group.ToList();
                for (var i = 0; i < teams.Count; i += 2)
                {
                    result.Add((teams[i], i + 1 < teams.Count ? teams[i + 1] : null));
                }
            }

            foreach (var unmatched in matchups.Where(m => m != null && m.matchup_id == null))
            {
                result.Add((unmatched, null));
            }

            return result;
        }

        [Command("roster", RunMode = RunMode.Async)]
        public async Task GetRoster([Remainder] string? manager = null)
        {
            try
            {
                var subscription = await RequireSubscription();
                if (subscription == null)
                    return;

                var context = await sleeperService.GetLeagueContext(subscription.LeagueId);

                if (string.IsNullOrWhiteSpace(manager))
                {
                    var eb = SleeperEmbeds.BaseEmbed(context.League);
                    eb.WithDescription("Specify a team to look up, e.g. `z fantasy roster " +
                                       (context.RosterIdToTeamName.Values.FirstOrDefault() ?? "name") + "`.");
                    eb.AddField("Teams", SleeperEmbeds.JoinCapped(context.RosterIdToTeamName.Values));
                    await Reply(eb);
                    return;
                }

                var matches = MatchRosters(context, manager);

                if (matches.Count == 0)
                {
                    await Reply($"No team matching \"{manager}\". Run `z fantasy roster` to list them.");
                    return;
                }

                if (matches.Count > 1)
                {
                    var names = matches.Select(r => context.RosterIdToTeamName[r.roster_id]);
                    await Reply($"That matches more than one team: {string.Join(", ", names)}.");
                    return;
                }

                var roster = matches[0];
                var starters = roster.starters ?? new List<string>();
                var reserve = roster.reserve ?? new List<string>();
                var bench = (roster.players ?? new List<string>())
                    .Except(starters)
                    .Except(reserve)
                    .ToList();

                var rosterEmbed = SleeperEmbeds.BaseEmbed(context.League);
                rosterEmbed.WithDescription(context.RosterIdToTeamName[roster.roster_id]);
                rosterEmbed.AddField("Starters",
                    SleeperEmbeds.JoinCapped(starters.Select(id => SleeperEmbeds.FormatRosterPlayer(id, sleeperService.GetPlayer))));
                rosterEmbed.AddField("Bench",
                    SleeperEmbeds.JoinCapped(bench.Select(id => SleeperEmbeds.FormatRosterPlayer(id, sleeperService.GetPlayer))));

                await Reply(rosterEmbed);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch Sleeper roster");
                await Reply($"Something went wrong. {ex.Message}");
            }
        }

        /// <summary>
        /// Case-insensitive substring match against team name, display name, and username.
        /// </summary>
        public static List<Roster> MatchRosters(SleeperService.LeagueContext context, string query)
        {
            var usersById = (context.Users ?? new List<LeagueUser>())
                .Where(u => u?.user_id != null)
                .GroupBy(u => u.user_id)
                .ToDictionary(g => g.Key, g => g.First());

            bool Matches(Roster roster)
            {
                var candidates = new List<string?>();

                if (context.RosterIdToTeamName.TryGetValue(roster.roster_id, out var teamName))
                    candidates.Add(teamName);

                if (roster.owner_id != null && usersById.TryGetValue(roster.owner_id, out var user))
                {
                    candidates.Add(user.display_name);
                    candidates.Add(user.username);
                    candidates.Add(user.metadata?.team_name);
                }

                return candidates.Any(c => !string.IsNullOrEmpty(c) &&
                                           c.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            return (context.Rosters ?? new List<Roster>()).Where(Matches).ToList();
        }

        [Command("bracket", RunMode = RunMode.Async)]
        public async Task GetBracket(string? which = null)
        {
            try
            {
                var subscription = await RequireSubscription();
                if (subscription == null)
                    return;

                var losers = string.Equals(which, "losers", StringComparison.OrdinalIgnoreCase);
                var context = await sleeperService.GetLeagueContext(subscription.LeagueId);

                var bracket = losers
                    ? await sleeperService.GetLosersBracket(subscription.LeagueId)
                    : await sleeperService.GetWinnersBracket(subscription.LeagueId);

                if (bracket == null || bracket.Count == 0)
                {
                    await Reply("The playoff bracket isn't available yet.");
                    return;
                }

                var eb = SleeperEmbeds.BaseEmbed(context.League);
                eb.WithDescription(losers ? "Consolation Bracket" : "Playoff Bracket");

                foreach (var round in bracket.GroupBy(b => b.r).OrderBy(g => g.Key))
                {
                    var lines = round.OrderBy(b => b.m)
                        .Select(b => FormatBracketMatch(b, context.RosterIdToTeamName));
                    eb.AddField($"Round {round.Key}", SleeperEmbeds.JoinCapped(lines));
                }

                await Reply(eb);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch Sleeper bracket");
                await Reply($"Something went wrong. {ex.Message}");
            }
        }

        /// <summary>
        /// One bracket line. A slot that is not yet decided renders as the match it
        /// feeds from, and a played match marks the winner.
        /// </summary>
        public static string FormatBracketMatch(BracketMatchup match, IReadOnlyDictionary<int, string>? rosterNames)
        {
            string Slot(int? rosterId, BracketMatchup.BracketSource? from)
            {
                if (rosterId.HasValue)
                {
                    var name = rosterNames != null && rosterNames.TryGetValue(rosterId.Value, out var n)
                        ? n
                        : $"Roster {rosterId.Value}";
                    return match.w.HasValue && match.w.Value == rosterId.Value ? $"{name} ✅" : name;
                }

                if (from?.w != null) return $"Winner of M{from.w.Value}";
                if (from?.l != null) return $"Loser of M{from.l.Value}";
                return "TBD";
            }

            return $"{Slot(match.t1, match.t1_from)} vs {Slot(match.t2, match.t2_from)}";
        }

        /// <summary>
        /// League-independent, so this is the one command that does not need a subscription.
        /// </summary>
        [Command("trending", RunMode = RunMode.Async)]
        public async Task GetTrending(string type = "add", int count = 10)
        {
            try
            {
                type = type?.ToLowerInvariant() == "drop" ? "drop" : "add";
                count = Math.Clamp(count, 1, 25);

                var trending = await sleeperService.GetTrending(type, 24, count);

                var eb = SleeperEmbeds.BaseEmbed(null);
                eb.WithTitle($"Trending {(type == "drop" ? "Drops" : "Adds")}");

                if (trending == null || trending.Count == 0)
                {
                    eb.WithDescription("Sleeper has no trending players right now.");
                }
                else
                {
                    var lines = trending.Select((t, i) =>
                    {
                        var player = sleeperService.GetPlayer(t.player_id);
                        var name = player?.DisplayName ?? t.player_id;
                        var detail = player == null ? string.Empty : $" ({player.position} - {player.team})";
                        return $"{i + 1}. {name}{detail} — {t.count:N0} {(type == "drop" ? "drops" : "adds")}";
                    });

                    eb.WithDescription(string.Join("\n", lines));
                }

                await Reply(eb);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch Sleeper trending players");
                await Reply($"Something went wrong. {ex.Message}");
            }
        }
    }
}
