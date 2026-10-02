using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Feed;

namespace SwiftBets.Offer.Application.Tests;

public sealed class StalenessGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Stale_feed_suspends_open_markets_and_fresh_data_reopens_exactly_those()
    {
        var (guard, offer, health, clock) = await BuildAsync(MarketStatus.Open, MarketStatus.Suspended);
        await health.RecordSeenAsync("fx-1", Now, CancellationToken.None);

        clock.SetUtcNow(Now.AddSeconds(121));
        await guard.CheckAsync(CancellationToken.None);
        (await offer.GetAsync("fx-1", CancellationToken.None))!.Markets.ShouldAllBe(m => m.Status == MarketStatus.Suspended);
        offer.StatusChanges.ShouldHaveSingleItem().Source.ShouldBe("staleness");

        await health.RecordSeenAsync("fx-1", clock.GetUtcNow(), CancellationToken.None);
        await guard.CheckAsync(CancellationToken.None);

        (await offer.GetAsync("fx-1", CancellationToken.None))!.Markets.Select(m => m.Status).ShouldBe([MarketStatus.Open, MarketStatus.Suspended]);
    }

    [Fact]
    public async Task Fresh_feed_leaves_the_offer_alone()
    {
        var (guard, offer, health, clock) = await BuildAsync(MarketStatus.Open, MarketStatus.Open);
        await health.RecordSeenAsync("fx-1", Now, CancellationToken.None);
        clock.SetUtcNow(Now.AddSeconds(119));

        (await guard.CheckAsync(CancellationToken.None)).ShouldBe(0);
        offer.StatusChanges.ShouldBeEmpty();
    }

    private static async Task<(StalenessGuard, InMemoryOffer, InMemoryFeedHealth, FakeTimeProvider)> BuildAsync(MarketStatus first, MarketStatus second)
    {
        var offer = new InMemoryOffer();
        var health = new InMemoryFeedHealth();
        var clock = new FakeTimeProvider(Now);
        SelectionV1[] selections = [new("home", "Arsenal", 2.1m)];
        await offer.TrySaveAsync(new FixtureChangedV1("fx-1", "Premier League", "Arsenal", "Chelsea", Now.AddHours(2), FixtureStatus.Scheduled, 1,
            [new MarketV1("fx-1-1x2", MarketType.MatchResult, first, selections), new MarketV1("fx-1-ou25", MarketType.TotalGoalsOverUnder25, second, selections)], Now), 0, CancellationToken.None);
        return (new StalenessGuard(offer, health, offer, Options.Create(new FeedOptions { StaleAfterSeconds = 120 }), clock), offer, health, clock);
    }
}
