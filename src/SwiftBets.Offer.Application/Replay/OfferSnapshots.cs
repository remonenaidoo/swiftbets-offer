using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Domain;

namespace SwiftBets.Offer.Application.Replay;

public static class OfferSnapshots
{
    public const string MatchResultSuffix = "1x2";
    public const string TotalGoalsSuffix = "ou25";

    public static IReadOnlyList<MarketV1> PricedMarkets(string fixtureId, HistoricalMatch match, double progress, MarketStatus status)
    {
        var o = match.Opening;
        var c = match.Closing;
        return
        [
            new MarketV1($"{fixtureId}-{MatchResultSuffix}", MarketType.MatchResult, status,
            [
                new SelectionV1("home", match.HomeTeam, PricePath.At(o.Home, c.Home, progress).Value),
                new SelectionV1("draw", "Draw", PricePath.At(o.Draw, c.Draw, progress).Value),
                new SelectionV1("away", match.AwayTeam, PricePath.At(o.Away, c.Away, progress).Value),
            ]),
            new MarketV1($"{fixtureId}-{TotalGoalsSuffix}", MarketType.TotalGoalsOverUnder25, status,
            [
                new SelectionV1("over", "Over 2.5", PricePath.At(o.Over25, c.Over25, progress).Value),
                new SelectionV1("under", "Under 2.5", PricePath.At(o.Under25, c.Under25, progress).Value),
            ]),
        ];
    }

    /// <summary>True when prices or statuses differ, ignoring the version and timestamp.</summary>
    public static bool OfferDiffers(FixtureChangedV1 current, FixtureStatus status, IReadOnlyList<MarketV1> markets) =>
        current.Status != status
        || current.Markets.Count != markets.Count
        || current.Markets.Zip(markets).Any(p => p.First.Status != p.Second.Status || !p.First.Selections.SequenceEqual(p.Second.Selections));
}
