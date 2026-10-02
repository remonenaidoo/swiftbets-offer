using Microsoft.Extensions.Logging;
using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Catalog;
using SwiftBets.Offer.Application.Markets;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Feed;

/// <summary>
/// Applies a feed poll to the offer store: a fixture is saved and published only when its status or prices change,
/// and each result version is published once. Operator suspensions survive a feed that still shows the market open;
/// only a resume reopens it. The feed's job is price correctness; availability and versioning are ours.
/// The catalogue is refreshed from the same poll in one batch; it is a read model, so its failure never stalls the offer.
/// </summary>
public sealed partial class FeedSync(IFeedAdapter feed, IOfferStore store, IOfferEvents events, IFeedHealthStore health, ICatalogStore catalog, TimeProvider time, ILogger<FeedSync> logger)
{
    public async Task<int> TickAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var poll = await feed.PollAsync(now, cancellationToken);
        var changes = 0;
        var catalogued = new List<CatalogFixture>(poll.Fixtures.Count);
        foreach (var fixture in poll.Fixtures)
        {
            var (changed, stored) = await ApplyFixtureAsync(fixture, now, cancellationToken);
            changes += changed;
            await health.RecordSeenAsync(fixture.FixtureId, fixture.UpdatedAt, cancellationToken);
            if (stored is not null)
            {
                catalogued.Add(new CatalogFixture(fixture.SportId, CatalogIds.Slug(stored.Competition), stored.Competition, stored.FixtureId, stored.HomeTeam, stored.AwayTeam,
                    stored.KickoffAt, stored.Status.ToString().ToLowerInvariant(), stored.OfferVersion));
            }
        }

        await RefreshCatalogAsync(catalogued, cancellationToken);

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

    private async Task RefreshCatalogAsync(List<CatalogFixture> fixtures, CancellationToken cancellationToken)
    {
        if (fixtures.Count == 0)
        {
            return;
        }

        try
        {
            await catalog.UpsertAsync(fixtures, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogCatalogFailed(ex, fixtures.Count);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Catalogue refresh of {Count} fixtures failed; the next tick retries it")]
    private partial void LogCatalogFailed(Exception exception, int count);

    private async Task<(int Changed, FixtureChangedV1? Stored)> ApplyFixtureAsync(FeedFixture fixture, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var current = await store.GetAsync(fixture.FixtureId, cancellationToken);
        var markets = fixture.Markets;
        if (current is not null)
        {
            var stored = current.Markets.ToDictionary(m => m.MarketId, m => m.Status, StringComparer.Ordinal);
            markets = [.. markets.Select(m => stored.TryGetValue(m.MarketId, out var was) ? m with { Status = MarketLifecycle.Next(was, m.Status) } : m)];
        }

        if (current is not null && fixture.Status == FixtureStatus.Scheduled)
        {
            var suspended = current.Markets.Where(m => m.Status == MarketStatus.Suspended).Select(m => m.MarketId).ToHashSet(StringComparer.Ordinal);
            markets = [.. markets.Select(m => m.Status == MarketStatus.Open && suspended.Contains(m.MarketId) ? m with { Status = MarketStatus.Suspended } : m)];
        }

        if (current is not null && !Differs(current, fixture.Status, markets))
        {
            return (0, current);
        }

        var snapshot = new FixtureChangedV1(fixture.FixtureId, fixture.Competition, fixture.HomeTeam, fixture.AwayTeam, fixture.KickoffAt, fixture.Status,
            (current?.OfferVersion ?? 0) + 1, markets, now);
        if (!await store.TrySaveAsync(snapshot, current?.OfferVersion ?? 0, cancellationToken))
        {
            return (0, null);
        }

        await events.FixtureChangedAsync(snapshot, cancellationToken);
        return (1, snapshot);
    }

    private static bool Differs(FixtureChangedV1 current, FixtureStatus status, IReadOnlyList<MarketV1> markets) =>
        current.Status != status
        || current.Markets.Count != markets.Count
        || current.Markets.Zip(markets).Any(p => p.First.MarketId != p.Second.MarketId || p.First.Status != p.Second.Status || !p.First.Selections.SequenceEqual(p.Second.Selections));
}
