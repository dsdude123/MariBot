# fantasy

Sleeper fantasy football integration.

Each server points at one Sleeper league. A moderator sets it once with `z fantasy subscribe`; everything else then works without arguments. Completed trades, waiver claims, and free agent pickups are announced automatically in the channel the subscription was created in.

Sleeper's API is public, so no login or API key is needed — you only need the league ID, which is the number in your league's URL: `https://sleeper.com/leagues/`**`289646328504385536`**.

## Setup

- `z fantasy subscribe <leagueId>` — Point this server at a Sleeper league and announce its transactions in the current channel. Requires the Manage Webhooks permission.
- `z fantasy unsubscribe` — Stop following the league and stop announcements. Requires the Manage Webhooks permission.

## Commands

- `z fantasy league` — League name, status, season, team count, current week, scoring format, and roster positions.
- `z fantasy standings` — Standings ordered by record, with points for and against.
- `z fantasy scoreboard [week]` — Scores for every matchup. Defaults to the current week.
- `z fantasy roster [team]` — Starters and bench for a team. Run without a team to list the teams in the league. Matching is case-insensitive and partial, against team name, display name, or username.
- `z fantasy bracket [losers]` — The playoff bracket. Add `losers` for the consolation bracket.
- `z fantasy trending [add|drop] [count]` — Most added or dropped players across all of Sleeper in the last 24 hours. Defaults to the top 10 adds. Works without a subscription.

## Notes

- Sleeper's public API does not expose projected points, so the scoreboard shows actual scores only.
- The Yahoo Fantasy Sports commands have moved to `z yahoofantasy` and are deprecated. See `z help yahoofantasy`.
