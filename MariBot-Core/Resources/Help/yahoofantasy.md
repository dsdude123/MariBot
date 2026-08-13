# yahoofantasy

Yahoo Fantasy Sports integration. **Deprecated** — the `fantasy` command group now points at Sleeper. See `z help fantasy`.

These commands still work, but no guild is mapped to a Yahoo league any more, so `league`, `standings`, and `scoreboard` have no league to read and the background transaction poller does nothing.

## Commands

- `z yahoofantasy league` — Display information about the current fantasy league.
- `z yahoofantasy standings` — Display the league standings for the current week.
- `z yahoofantasy scoreboard` — Display current and projected scores for all matchups.
- `z yahoofantasy authenticate [code]` — Authenticate with Yahoo Fantasy Sports. Run without a code first to receive the authorization link, then re-run with the provided code.
- `z yahoofantasy raw <url>` — Fetch a raw API response from a Yahoo Fantasy Sports endpoint.
