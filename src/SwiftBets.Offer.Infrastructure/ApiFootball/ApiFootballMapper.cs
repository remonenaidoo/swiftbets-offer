using System.Globalization;
using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Feed;
using SwiftBets.Offer.Application.Replay;
using SwiftBets.Offer.Domain;

namespace SwiftBets.Offer.Infrastructure.ApiFootball;

/// <summary>Maps the provider's fixtures and odds onto our markets: the same ids and selections the replay produces.</summary>
internal static class ApiFootballMapper
{
    public const int MatchWinnerBet = 1;
    public const int GoalsOverUnderBet = 5;

    private static readonly HashSet<string> NotStarted = ["NS", "TBD"];
    private static readonly HashSet<string> Finished = ["FT", "AET", "PEN", "AWD", "WO"];
    private static readonly HashSet<string> CalledOff = ["PST", "CANC", "ABD"];

    public static FeedPoll Map(IReadOnlyList<FixtureItem> fixtures, IReadOnlyList<OddsItem> odds)
    {
        var oddsById = odds.GroupBy(o => o.Fixture.Id).ToDictionary(g => g.Key, g => g.MaxBy(o => o.Update)!);
        var feedFixtures = new List<FeedFixture>();
        var results = new List<FeedResult>();
        foreach (var item in fixtures)
        {
            var id = FixtureId(item.Fixture.Id);
            var status = StatusOf(item.Fixture.Status.Short);
            var priced = oddsById.GetValueOrDefault(item.Fixture.Id);
            var marketStatus = status == FixtureStatus.Scheduled ? MarketStatus.Open : status == FixtureStatus.InPlay ? MarketStatus.Suspended : MarketStatus.Closed;
            var markets = priced is null ? [] : Markets(id, item.Teams, priced, marketStatus);
            if (markets.Count == 0 && status == FixtureStatus.Scheduled)
            {
                continue;
            }

            feedFixtures.Add(new FeedFixture(id, item.League.Name, item.Teams.Home.Name, item.Teams.Away.Name, item.Fixture.Date, status, markets,
                priced?.Update ?? DateTimeOffset.MinValue));

            var score = item.Score?.Fulltime is { Home: not null, Away: not null } fulltime ? fulltime : item.Goals;
            if (Finished.Contains(item.Fixture.Status.Short) && score is { Home: { } home, Away: { } away })
            {
                results.Add(new FeedResult(id, 1, ResultStatus.Official, home, away));
            }
            else if (CalledOff.Contains(item.Fixture.Status.Short) && item.Fixture.Status.Short != "PST")
            {
                results.Add(new FeedResult(id, 1, ResultStatus.Void, 0, 0));
            }
        }

        return new FeedPoll(feedFixtures, results);
    }

    public static string FixtureId(long providerId) => $"af-{providerId.ToString(CultureInfo.InvariantCulture)}";

    private static FixtureStatus StatusOf(string code) =>
        NotStarted.Contains(code) ? FixtureStatus.Scheduled
        : Finished.Contains(code) ? FixtureStatus.Finished
        : CalledOff.Contains(code) ? FixtureStatus.Postponed
        : FixtureStatus.InPlay;

    private static List<MarketV1> Markets(string fixtureId, TeamPair teams, OddsItem odds, MarketStatus status)
    {
        var markets = new List<MarketV1>();
        foreach (var bookmaker in odds.Bookmakers)
        {
            var winner = bookmaker.Bets.FirstOrDefault(b => b.Id == MatchWinnerBet);
            if (winner is not null && markets.All(m => m.Type != MarketType.MatchResult)
                && Price(winner, "Home") is { } home && Price(winner, "Draw") is { } draw && Price(winner, "Away") is { } away)
            {
                markets.Add(new MarketV1($"{fixtureId}-{OfferSnapshots.MatchResultSuffix}", MarketType.MatchResult, status,
                    [new SelectionV1("home", teams.Home.Name, home), new SelectionV1("draw", "Draw", draw), new SelectionV1("away", teams.Away.Name, away)]));
            }

            var goals = bookmaker.Bets.FirstOrDefault(b => b.Id == GoalsOverUnderBet);
            if (goals is not null && markets.All(m => m.Type != MarketType.TotalGoalsOverUnder25)
                && Price(goals, "Over 2.5") is { } over && Price(goals, "Under 2.5") is { } under)
            {
                markets.Add(new MarketV1($"{fixtureId}-{OfferSnapshots.TotalGoalsSuffix}", MarketType.TotalGoalsOverUnder25, status,
                    [new SelectionV1("over", "Over 2.5", over), new SelectionV1("under", "Under 2.5", under)]));
            }
        }

        return [.. markets.OrderBy(m => m.Type)];
    }

    /// <summary>A price we can offer, or null: missing, unparseable or below the minimum odds all mean no market.</summary>
    private static decimal? Price(Bet bet, string value) =>
        bet.Values.FirstOrDefault(v => v.Value == value) is { } found
        && decimal.TryParse(found.Odd, NumberStyles.Number, CultureInfo.InvariantCulture, out var odd) && odd >= Odds.Minimum
            ? Odds.From(odd).Value
            : null;
}
