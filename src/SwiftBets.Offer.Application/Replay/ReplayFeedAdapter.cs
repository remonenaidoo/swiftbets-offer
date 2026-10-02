using Microsoft.Extensions.Options;
using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Feed;
using SwiftBets.Offer.Application.Ports;
using SwiftBets.Offer.Domain;

namespace SwiftBets.Offer.Application.Replay;

/// <summary>
/// The recorded EPL season as a feed, on a compressed clock: fixtures list ahead of kickoff, prices drift from opening
/// to closing, results follow, and every Nth match gets a result correction. Deterministic from the clock.
/// </summary>
public sealed class ReplayFeedAdapter(ISeasonSource season, IOptions<ReplayOptions> options) : IFeedAdapter
{
    private readonly ReplayTimeline _timeline = new(
        season.Season,
        season.Matches,
        options.Value.Epoch,
        TimeSpan.FromSeconds(options.Value.SlotSeconds),
        options.Value.ListLeadSlots);

    public string Name => "replay";

    public Task<FeedPoll> PollAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var fixtures = new List<FeedFixture>();
        var results = new List<FeedResult>();
        foreach (var slot in _timeline.ActiveAt(now, TimeSpan.FromSeconds(options.Value.SlotSeconds * 3)))
        {
            var phase = slot.PhaseAt(now);
            var status = phase == FixturePhase.Open ? FixtureStatus.Scheduled : phase == FixturePhase.AwaitingResult ? FixtureStatus.InPlay : FixtureStatus.Finished;
            var marketStatus = phase == FixturePhase.Open ? MarketStatus.Open : phase == FixturePhase.AwaitingResult ? MarketStatus.Suspended : MarketStatus.Closed;
            fixtures.Add(new FeedFixture(slot.FixtureId, season.Competition, slot.Match.HomeTeam, slot.Match.AwayTeam, slot.KickoffAt, status,
                OfferSnapshots.PricedMarkets(slot.FixtureId, slot.Match, slot.PriceProgressAt(now), marketStatus)));

            if (phase == FixturePhase.Finished)
            {
                results.Add(new FeedResult(slot.FixtureId, 1, ResultStatus.Official, slot.Match.HomeGoals, slot.Match.AwayGoals));
                if (IsCorrected(slot.Match) && now >= slot.ResultAt + (slot.ResultAt - slot.KickoffAt))
                {
                    var (home, away) = CorrectedScore(slot.Match);
                    results.Add(new FeedResult(slot.FixtureId, 2, ResultStatus.Correction, home, away));
                }
            }
        }

        return Task.FromResult(new FeedPoll(fixtures, results));
    }

    private bool IsCorrected(HistoricalMatch match) =>
        options.Value.CorrectionEvery > 0 && match.Index % options.Value.CorrectionEvery == options.Value.CorrectionEvery - 1;

    /// <summary>A corrected score that changes the match result: a draw gains a late home goal; otherwise the goals swap sides.</summary>
    private static (int Home, int Away) CorrectedScore(HistoricalMatch match) =>
        match.HomeGoals == match.AwayGoals ? (match.HomeGoals + 1, match.AwayGoals) : (match.AwayGoals, match.HomeGoals);
}
