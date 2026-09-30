using SwiftBets.Contracts.Errors;
using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Markets;

namespace SwiftBets.Offer.Application.Tests;

public sealed class SetMarketStatusHandlerTests
{
    [Fact]
    public async Task Suspending_a_market_bumps_the_offer_version_and_publishes()
    {
        var offer = await SeededAsync();

        var result = await new SetMarketStatusHandler(offer, offer, TimeProvider.System).HandleAsync("fx", "fx-1x2", suspend: true, CancellationToken.None);

        result.Value.OfferVersion.ShouldBe(2);
        result.Value.Markets.Single().Status.ShouldBe(MarketStatus.Suspended);
        offer.Published.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Unknown_market_is_not_found()
    {
        var offer = await SeededAsync();

        var result = await new SetMarketStatusHandler(offer, offer, TimeProvider.System).HandleAsync("fx", "nope", suspend: true, CancellationToken.None);

        result.Error!.Kind.ShouldBe(ErrorKind.NotFound);
    }

    private static async Task<InMemoryOffer> SeededAsync()
    {
        var offer = new InMemoryOffer();
        var market = new MarketV1("fx-1x2", MarketType.MatchResult, MarketStatus.Open, [new SelectionV1("home", "Arsenal", 2m)]);
        await offer.TrySaveAsync(new FixtureChangedV1("fx", "PL", "Arsenal", "Chelsea", DateTimeOffset.UtcNow.AddHours(1), FixtureStatus.Scheduled, 1, [market], DateTimeOffset.UtcNow), 0, CancellationToken.None);
        return offer;
    }
}
