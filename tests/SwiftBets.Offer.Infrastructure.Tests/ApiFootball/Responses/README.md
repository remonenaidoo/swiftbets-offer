Response bodies for the API-Football contract tests, one file per call the adapter makes.

They follow the provider's published v3 response schema (envelope, fixture, league, teams, goals, score, odds update, bookmakers, bets, values) with bet ids 1 (Match Winner) and 5 (Goals Over/Under). `quota-exceeded.json` is the documented 200-with-errors body for a spent daily quota.

Run `scripts/record-api-football.sh` with `API_FOOTBALL_KEY` set to replace them with live recordings; the tests must stay green on the recorded bodies.
