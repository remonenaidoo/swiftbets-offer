#!/usr/bin/env bash
# Records live API-Football responses over the contract-test bodies (4 requests). Needs API_FOOTBALL_KEY.
set -euo pipefail
: "${API_FOOTBALL_KEY:?set API_FOOTBALL_KEY}"
league="${LEAGUE:-39}"; season="${SEASON:-2026}"
out="$(cd "$(dirname "$0")/.." && pwd)/tests/SwiftBets.Offer.Infrastructure.Tests/ApiFootball/Responses"
get() { curl -fsS -H "x-apisports-key: $API_FOOTBALL_KEY" "https://v3.football.api-sports.io/$1" -o "$out/$2"; echo "recorded $2"; }
get "fixtures?league=$league&season=$season&from=$(date -u -d yesterday +%F)&to=$(date -u -d '+7 days' +%F)" fixtures.json
get "odds?league=$league&season=$season&bet=1" odds-match-winner.json
get "odds?league=$league&season=$season&bet=5" odds-goals-over-under.json
echo "Point the contract tests' fixture ids at ids present in the recording, then run them."
