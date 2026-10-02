using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Feed;

/// <summary>
/// Applies a feed poll to the offer store: a fixture is saved and published only when its status or prices change,
/// and each result version is published once. Operator suspensions survive a feed that still shows the market open;
/// only a resume reopens it. The feed's job is price correctness; availability and versioning are ours.
/// </summary>
public sealed class FeedSync(IFeedAdapter feed, IOfferStore store, IOfferEvents events, TimeProvider time)
{
    public async Task<int> TickAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var poll = await feed.PollAsync(now, cancellationToken);
        var changes = 0;
        foreach (var fixture in poll.Fixtures)
        {
            changes += await ApplyFixtureAsync(fixture, now, cancellationToken);
        }

        foreach (var result in poll.Results)
        {
            if (!await store.IsResultPublishedAsync(result.FixtureId, result.Version, cancellationToken))
            {
                await events.ResultPublishedAsync(new ResultPublishedV1(result.FixtureId, result.Version, result.Status, result.HomeGoals, result.AwayGoals, now), cancellationToken);
                await store.MarkResultPublishedAsync(result.FixtureId, result.Version, cancellationToken);
                changes++;
            }
        }

        return changes;
    }

    private async Task<int> ApplyFixtureAsync(FeedFixture fixture, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var current = await store.GetAsync(fixture.FixtureId, cancellationToken);
        var markets = fixture.Markets;
        if (current is not null && fixture.Status == FixtureStatus.Scheduled)
        {
            var suspended = current.Markets.Where(m => m.Status == MarketStatus.Suspended).Select(m => m.MarketId).ToHashSet(StringComparer.Ordinal);
            markets = [.. markets.Select(m => m.Status == MarketStatus.Open && suspended.Contains(m.MarketId) ? m with { Status = MarketStatus.Suspended } : m)];
        }

        if (current is not null && !Differs(current, fixture.Status, markets))
        {
            return 0;
        }

        var snapshot = new FixtureChangedV1(fixture.FixtureId, fixture.Competition, fixture.HomeTeam, fixture.AwayTeam, fixture.KickoffAt, fixture.Status,
            (current?.OfferVersion ?? 0) + 1, markets, now);
        if (!await store.TrySaveAsync(snapshot, current?.OfferVersion ?? 0, cancellationToken))
        {
            return 0;
        }

        await events.FixtureChangedAsync(snapshot, cancellationToken);
        return 1;
    }

    private static bool Differs(FixtureChangedV1 current, FixtureStatus status, IReadOnlyList<MarketV1> markets) =>
        current.Status != status
        || current.Markets.Count != markets.Count
        || current.Markets.Zip(markets).Any(p => p.First.MarketId != p.Second.MarketId || p.First.Status != p.Second.Status || !p.First.Selections.SequenceEqual(p.Second.Selections));
}
