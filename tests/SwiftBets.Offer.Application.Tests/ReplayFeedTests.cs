using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SwiftBets.Offer.Application.Feed;
using SwiftBets.Offer.Application.Ports;
using SwiftBets.Offer.Application.Replay;
using SwiftBets.Offer.Domain;

namespace SwiftBets.Offer.Application.Tests;

public sealed class ReplayFeedTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Fixture_is_listed_then_resulted_exactly_once()
    {
        var (engine, offer, clock) = Build();

        clock.SetUtcNow(Epoch.AddSeconds(5));
        await engine.TickAsync(CancellationToken.None);
        clock.SetUtcNow(Epoch.AddSeconds(25));
        await engine.TickAsync(CancellationToken.None);
        await engine.TickAsync(CancellationToken.None);

        offer.Published.ShouldContain(f => f.OfferVersion == 1 && f.Status == Contracts.Offer.FixtureStatus.Scheduled);
        offer.Results.Count.ShouldBe(1);
        offer.Results[0].HomeGoals.ShouldBe(2);
    }

    [Fact]
    public async Task Unchanged_offer_is_not_republished()
    {
        var (engine, offer, clock) = Build();
        clock.SetUtcNow(Epoch.AddSeconds(5));
        await engine.TickAsync(CancellationToken.None);

        (await engine.TickAsync(CancellationToken.None)).ShouldBe(0);
        offer.Published.Count.ShouldBe(1);
    }

    [Fact]
    public async Task An_operator_suspension_survives_a_feed_that_still_prices_the_market_open()
    {
        var (engine, offer, clock) = Build();
        clock.SetUtcNow(Epoch.AddSeconds(5));
        await engine.TickAsync(CancellationToken.None);
        var listed = offer.Published[^1];
        await offer.TrySaveAsync(listed with { OfferVersion = 2, Markets = [.. listed.Markets.Select(m => m with { Status = Contracts.Offer.MarketStatus.Suspended })] }, 1, CancellationToken.None);

        clock.SetUtcNow(Epoch.AddSeconds(6));
        await engine.TickAsync(CancellationToken.None);

        (await offer.GetAsync(listed.FixtureId, CancellationToken.None))!.Markets.ShouldAllBe(m => m.Status == Contracts.Offer.MarketStatus.Suspended);
    }

    [Fact]
    public async Task Designated_match_has_its_result_corrected_once_with_a_different_outcome()
    {
        var (engine, offer, clock) = Build(correctionEvery: 1);

        foreach (var seconds in new[] { 5, 25, 35, 36 })
        {
            clock.SetUtcNow(Epoch.AddSeconds(seconds));
            await engine.TickAsync(CancellationToken.None);
        }

        offer.Results.Select(r => (r.ResultVersion, r.Status, r.HomeGoals, r.AwayGoals)).ShouldBe([(1, Contracts.Offer.ResultStatus.Official, 2, 1), (2, Contracts.Offer.ResultStatus.Correction, 1, 2)]);
    }

    [Fact]
    public async Task Match_not_designated_for_correction_keeps_its_official_result()
    {
        var (engine, offer, clock) = Build(correctionEvery: 0);

        foreach (var seconds in new[] { 5, 25, 35 })
        {
            clock.SetUtcNow(Epoch.AddSeconds(seconds));
            await engine.TickAsync(CancellationToken.None);
        }

        offer.Results.ShouldHaveSingleItem().ResultVersion.ShouldBe(1);
    }

    private static (FeedSync Engine, InMemoryOffer Offer, FakeTimeProvider Clock) Build(int correctionEvery = 0)
    {
        var offer = new InMemoryOffer();
        var clock = new FakeTimeProvider(Epoch);
        var options = Options.Create(new ReplayOptions { SlotSeconds = 10, ListLeadSlots = 1, Epoch = Epoch, CorrectionEvery = correctionEvery });
        return (new FeedSync(new ReplayFeedAdapter(new OneMatchSeason(), options), offer, offer, clock), offer, clock);
    }

    private sealed class OneMatchSeason : ISeasonSource
    {
        private static readonly HistoricalPrices Prices = new(2m, 3.4m, 4m, 1.9m, 1.9m);

        public string Competition => "Premier League";

        public string Season => "test";

        public IReadOnlyList<HistoricalMatch> Matches { get; } = [new(0, "Arsenal", "Chelsea", 2, 1, Prices, Prices)];
    }
}
