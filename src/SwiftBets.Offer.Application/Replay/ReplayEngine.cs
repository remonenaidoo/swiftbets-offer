using Microsoft.Extensions.Options;
using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Ports;
using SwiftBets.Offer.Domain;

namespace SwiftBets.Offer.Application.Replay;

/// <summary>
/// One tick of the replay: brings every active fixture's stored offer in line with the timeline, publishing a snapshot
/// whenever it changes and a result once per fixture. Deterministic from the clock, so a restart simply converges.
/// Operator suspensions survive: a suspended market stays suspended until resumed.
/// </summary>
public sealed class ReplayEngine(ISeasonSource season, IOfferStore store, IOfferEvents events, IOptions<ReplayOptions> options, TimeProvider time)
{
    private readonly ReplayTimeline _timeline = new(
        season.Season,
        season.Matches,
        options.Value.Epoch,
        TimeSpan.FromSeconds(options.Value.SlotSeconds),
        options.Value.ListLeadSlots);

    public async Task<int> TickAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var changes = 0;
        foreach (var slot in _timeline.ActiveAt(now, TimeSpan.FromSeconds(options.Value.SlotSeconds * 3)))
        {
            changes += await SyncAsync(slot, now, cancellationToken);
        }

        return changes;
    }

    private async Task<int> SyncAsync(ReplaySlot slot, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var phase = slot.PhaseAt(now);
        var current = await store.GetAsync(slot.FixtureId, cancellationToken);
        var status = phase == FixturePhase.Open ? FixtureStatus.Scheduled : phase == FixturePhase.AwaitingResult ? FixtureStatus.InPlay : FixtureStatus.Finished;
        var marketStatus = phase == FixturePhase.Open ? MarketStatus.Open : phase == FixturePhase.AwaitingResult ? MarketStatus.Suspended : MarketStatus.Closed;
        var markets = OfferSnapshots.PricedMarkets(slot.FixtureId, slot.Match, slot.PriceProgressAt(now), marketStatus);
        if (current is not null && phase == FixturePhase.Open)
        {
            markets = [.. markets.Zip(current.Markets, (next, stored) => stored.Status == MarketStatus.Suspended ? next with { Status = MarketStatus.Suspended } : next)];
        }

        var changes = 0;
        if (current is null || OfferSnapshots.OfferDiffers(current, status, markets))
        {
            var version = (current?.OfferVersion ?? 0) + 1;
            var snapshot = new FixtureChangedV1(slot.FixtureId, season.Competition, slot.Match.HomeTeam, slot.Match.AwayTeam, slot.KickoffAt, status, version, markets, now);
            if (await store.TrySaveAsync(snapshot, current?.OfferVersion ?? 0, cancellationToken))
            {
                await events.FixtureChangedAsync(snapshot, cancellationToken);
                changes++;
            }
        }

        if (phase == FixturePhase.Finished && !await store.IsResultPublishedAsync(slot.FixtureId, 1, cancellationToken))
        {
            await events.ResultPublishedAsync(
                new ResultPublishedV1(slot.FixtureId, 1, ResultStatus.Official, slot.Match.HomeGoals, slot.Match.AwayGoals, now),
                cancellationToken);
            await store.MarkResultPublishedAsync(slot.FixtureId, 1, cancellationToken);
            changes++;
        }

        if (IsCorrected(slot.Match) && now >= slot.ResultAt + (slot.ResultAt - slot.KickoffAt)
            && await store.IsResultPublishedAsync(slot.FixtureId, 1, cancellationToken)
            && !await store.IsResultPublishedAsync(slot.FixtureId, 2, cancellationToken))
        {
            var (home, away) = CorrectedScore(slot.Match);
            await events.ResultPublishedAsync(new ResultPublishedV1(slot.FixtureId, 2, ResultStatus.Correction, home, away, now), cancellationToken);
            await store.MarkResultPublishedAsync(slot.FixtureId, 2, cancellationToken);
            changes++;
        }

        return changes;
    }

    private bool IsCorrected(HistoricalMatch match) =>
        options.Value.CorrectionEvery > 0 && match.Index % options.Value.CorrectionEvery == options.Value.CorrectionEvery - 1;

    /// <summary>A corrected score that changes the match result: a draw gains a late home goal; otherwise the goals swap sides.</summary>
    private static (int Home, int Away) CorrectedScore(HistoricalMatch match) =>
        match.HomeGoals == match.AwayGoals ? (match.HomeGoals + 1, match.AwayGoals) : (match.AwayGoals, match.HomeGoals);
}
