using Discord;
using MariBot.Core.Models.Sleeper;

namespace MariBot.Core.Utils
{
    /// <summary>
    /// Builders for the Sleeper embeds. These are static and take their player lookup
    /// as a delegate so that everything worth testing — transaction rendering, field
    /// truncation, matchup pairing — sits on the pure side of both the HTTP boundary
    /// and the Discord context.
    /// </summary>
    public static class SleeperEmbeds
    {
        /// <summary>Discord's hard limit on the length of a single embed field value.</summary>
        public const int FieldValueLimit = 1024;

        public static readonly EmbedFooterBuilder SleeperFooter = new EmbedFooterBuilder()
            .WithText("Sleeper")
            .WithIconUrl("https://sleepercdn.com/images/v2/icons/player_default.webp");

        public static string LeagueUrl(string leagueId) => $"https://sleeper.com/leagues/{leagueId}";

        public static string? AvatarUrl(string avatar) =>
            string.IsNullOrEmpty(avatar) ? null : $"https://sleepercdn.com/avatars/{avatar}";

        /// <summary>
        /// Starts an embed with the branding every Sleeper command shares. The league is
        /// optional because <c>trending</c> is league-independent.
        /// </summary>
        public static EmbedBuilder BaseEmbed(League? league)
        {
            var eb = new EmbedBuilder()
                .WithFooter(SleeperFooter)
                .WithColor(new Color(0x00, 0xCE, 0xB8));

            if (league != null)
            {
                eb.WithTitle(league.name);
                eb.WithUrl(LeagueUrl(league.league_id));

                var avatar = AvatarUrl(league.avatar);
                if (avatar != null)
                    eb.WithThumbnailUrl(avatar);
            }

            return eb;
        }

        /// <summary>
        /// "Josh Allen - BUF - QB", the form used in transaction announcements.
        /// Unknown ids render as the raw id so a cold catalog never blanks a line.
        /// </summary>
        public static string FormatTransactionPlayer(string playerId, Func<string, SleeperPlayer?>? lookup)
        {
            var player = lookup?.Invoke(playerId);
            if (player == null)
                return playerId;

            var parts = new List<string> { player.DisplayName };
            if (!string.IsNullOrEmpty(player.team)) parts.Add(player.team);
            if (!string.IsNullOrEmpty(player.position)) parts.Add(player.position);
            return string.Join(" - ", parts);
        }

        /// <summary>
        /// "QB Josh Allen (BUF)" with an injury suffix, the form used in roster listings.
        /// </summary>
        public static string FormatRosterPlayer(string playerId, Func<string, SleeperPlayer?>? lookup)
        {
            var player = lookup?.Invoke(playerId);
            if (player == null)
                return playerId;

            var line = $"{player.position} {player.DisplayName}";
            if (!string.IsNullOrEmpty(player.team))
                line += $" ({player.team})";
            if (!string.IsNullOrEmpty(player.injury_status))
                line += $" — {player.injury_status}";
            return line;
        }

        /// <summary>
        /// Joins lines into an embed field value, trimming to Discord's 1024-character
        /// limit. A full bench of long names overruns it, and Discord rejects the whole
        /// message rather than truncating for us, so the overflow is counted instead.
        /// </summary>
        public static string JoinCapped(IEnumerable<string>? lines, int limit = FieldValueLimit)
        {
            var list = lines?.ToList() ?? new List<string>();
            if (list.Count == 0)
                return "None";

            var kept = new List<string>();
            var length = 0;

            for (var i = 0; i < list.Count; i++)
            {
                var line = list[i];
                var addition = kept.Count == 0 ? line.Length : line.Length + 1;
                var remaining = list.Count - i;

                // Reserve room for the "…and N more" line before committing to this one.
                var suffix = $"…and {remaining} more";
                var suffixCost = kept.Count == 0 ? suffix.Length : suffix.Length + 1;

                if (length + addition > limit || (length + addition + suffixCost > limit && remaining > 1))
                {
                    kept.Add($"…and {remaining} more");
                    break;
                }

                kept.Add(line);
                length += addition;
            }

            var result = string.Join("\n", kept);
            return result.Length > limit ? result.Substring(0, limit) : result;
        }

        /// <summary>
        /// Announcement embed for a completed transaction.
        /// </summary>
        /// <param name="transaction">The transaction to render.</param>
        /// <param name="league">League the transaction belongs to, for branding.</param>
        /// <param name="rosterNames">roster_id to team name, from the league context.</param>
        /// <param name="lookup">Player catalog lookup; may return null on a miss.</param>
        public static EmbedBuilder BuildTransactionEmbed(
            Transaction transaction,
            League league,
            IReadOnlyDictionary<int, string>? rosterNames,
            Func<string, SleeperPlayer?>? lookup)
        {
            return transaction.type == "trade"
                ? BuildTradeEmbed(transaction, league, rosterNames, lookup)
                : BuildAddDropEmbed(transaction, league, rosterNames, lookup);
        }

        private static string TeamName(IReadOnlyDictionary<int, string>? rosterNames, int rosterId)
        {
            return rosterNames != null && rosterNames.TryGetValue(rosterId, out var name)
                ? name
                : $"Roster {rosterId}";
        }

        private static EmbedBuilder BuildAddDropEmbed(
            Transaction transaction,
            League league,
            IReadOnlyDictionary<int, string>? rosterNames,
            Func<string, SleeperPlayer?>? lookup)
        {
            var eb = BaseEmbed(league);

            // adds/drops map player_id to the roster involved, so the acting team comes
            // from those values rather than from roster_ids, which does not say who
            // received what.
            var actingRoster = transaction.adds?.Values.FirstOrDefault()
                               ?? transaction.drops?.Values.FirstOrDefault()
                               ?? transaction.roster_ids?.FirstOrDefault()
                               ?? 0;

            var kind = transaction.type == "waiver" ? "waiver" : "free agent";
            eb.WithDescription($"{TeamName(rosterNames, actingRoster)} completed a {kind} transaction");

            if (transaction.adds != null && transaction.adds.Count > 0)
            {
                var bid = transaction.settings?.waiver_bid;
                var lines = transaction.adds.Keys
                    .Select(id => FormatTransactionPlayer(id, lookup) + (bid.HasValue ? $" (${bid.Value})" : string.Empty));
                eb.AddField("Adds", JoinCapped(lines));
            }

            if (transaction.drops != null && transaction.drops.Count > 0)
            {
                var lines = transaction.drops.Keys.Select(id => FormatTransactionPlayer(id, lookup));
                eb.AddField("Drops", JoinCapped(lines));
            }

            return eb;
        }

        private static EmbedBuilder BuildTradeEmbed(
            Transaction transaction,
            League league,
            IReadOnlyDictionary<int, string>? rosterNames,
            Func<string, SleeperPlayer?>? lookup)
        {
            var eb = BaseEmbed(league);

            var rosterIds = transaction.roster_ids ?? new List<int>();
            var names = rosterIds.Select(id => TeamName(rosterNames, id)).ToList();
            eb.WithDescription(names.Count > 0
                ? $"{string.Join(", ", names)} have completed a trade"
                : "A trade has been completed");

            // Sleeper supports trades between three or more rosters, so this iterates
            // the participants rather than assuming a trader/tradee pair.
            foreach (var rosterId in rosterIds)
            {
                var lines = new List<string>();

                if (transaction.adds != null)
                {
                    lines.AddRange(transaction.adds
                        .Where(kv => kv.Value == rosterId)
                        .Select(kv => FormatTransactionPlayer(kv.Key, lookup)));
                }

                if (transaction.draft_picks != null)
                {
                    lines.AddRange(transaction.draft_picks
                        .Where(p => p.owner_id == rosterId)
                        .Select(p => $"{p.season} Round {p.round} pick"));
                }

                eb.AddField($"{TeamName(rosterNames, rosterId)} receives", JoinCapped(lines));
            }

            return eb;
        }
    }
}
