using Microsoft.Extensions.Options;
using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Trading;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Feed;

/// <summary>
/// Never take bets on prices the feed has stopped updating: a scheduled fixture whose feed data goes stale has its open
/// markets suspended, and once fresh data arrives exactly those markets reopen. A trader's suspension is not touched.
/// Runs every tick, even when the poll itself failed, because an outage is exactly when it matters.
/// </summary>
public sealed class StalenessGuard(IOfferStore store, IFeedHealthStore health, IOfferEvents events, IOptions<FeedOptions> options, TimeProvider time)
{
    public async Task<int> CheckAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var staleAfter = TimeSpan.FromSeconds(options.Value.StaleAfterSeconds);
        var changes = 0;
        foreach (var fixture in await store.ListOpenAsync(now, 200, cancellationToken))
        {
            if (fixture.Status != FixtureStatus.Scheduled || await health.GetSeenAsync(fixture.FixtureId, cancellationToken) is not { } seen)
            {
                continue;
            }

            var suspendedByStaleness = await health.GetStaleSuspendedAsync(fixture.FixtureId, cancellationToken);
            if (now - seen > staleAfter && suspendedByStaleness is null)
            {
                changes += await SuspendAsync(fixture, staleAfter, now, cancellationToken);
            }
            else if (now - seen <= staleAfter && suspendedByStaleness is not null)
            {
                changes += await ResumeAsync(fixture, suspendedByStaleness, now, cancellationToken);
            }
        }

        return changes;
    }

    private async Task<int> SuspendAsync(FixtureChangedV1 fixture, TimeSpan staleAfter, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var open = fixture.Markets.Where(m => m.Status == MarketStatus.Open).Select(m => m.MarketId).ToList();
        if (open.Count == 0 || !await SaveAsync(fixture, open, MarketStatus.Suspended, now, cancellationToken))
        {
            return 0;
        }

        await health.SetStaleSuspendedAsync(fixture.FixtureId, open, cancellationToken);
        foreach (var marketId in open)
        {
            await events.MarketStatusChangedAsync(new MarketStatusChangedV1(fixture.FixtureId, marketId, MarketStatus.Suspended, "staleness",
                $"feed data older than {staleAfter.TotalSeconds:0}s", null, now), cancellationToken);
        }

        return 1;
    }

    private async Task<int> ResumeAsync(FixtureChangedV1 fixture, IReadOnlyList<string> suspendedByStaleness, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var reopen = fixture.Markets.Where(m => m.Status == MarketStatus.Suspended && suspendedByStaleness.Contains(m.MarketId)).Select(m => m.MarketId).ToList();
        if (reopen.Count > 0 && !await SaveAsync(fixture, reopen, MarketStatus.Open, now, cancellationToken))
        {
            return 0;
        }

        await health.ClearStaleSuspendedAsync(fixture.FixtureId, cancellationToken);
        foreach (var marketId in reopen)
        {
            await events.MarketStatusChangedAsync(new MarketStatusChangedV1(fixture.FixtureId, marketId, MarketStatus.Open, "staleness", "feed data fresh again", null, now), cancellationToken);
        }

        return reopen.Count > 0 ? 1 : 0;
    }

    private async Task<bool> SaveAsync(FixtureChangedV1 fixture, IReadOnlyList<string> marketIds, MarketStatus status, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var next = fixture with
        {
            OfferVersion = fixture.OfferVersion + 1,
            ChangedAt = now,
            Markets = [.. fixture.Markets.Select(m => marketIds.Contains(m.MarketId) ? m with { Status = status } : m)],
        };
        if (!await store.TrySaveAsync(next, fixture.OfferVersion, cancellationToken))
        {
            return false;
        }

        await events.FixtureChangedAsync(next, cancellationToken);
        return true;
    }
}
