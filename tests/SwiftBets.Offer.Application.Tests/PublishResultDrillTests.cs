using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Drills;

namespace SwiftBets.Offer.Application.Tests;

public sealed class PublishResultDrillTests
{
    [Fact]
    public async Task A_drill_publishes_the_result_at_the_version_asked_for()
    {
        var offer = await SeededAsync();

        var result = await new PublishResultDrill(offer, offer, TimeProvider.System).HandleAsync(new("fx-1", 6, ResultStatus.Correction, 0, 1), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        offer.Results.ShouldHaveSingleItem().ResultVersion.ShouldBe(6);
    }

    [Fact]
    public async Task A_drill_on_a_fixture_not_on_offer_publishes_nothing()
    {
        var offer = await SeededAsync();

        var result = await new PublishResultDrill(offer, offer, TimeProvider.System).HandleAsync(new("fx-404", 1, ResultStatus.Official, 1, 0), CancellationToken.None);

        (result.IsSuccess, result.Error!.Code).ShouldBe((false, "fixture_not_found"));
        offer.Results.ShouldBeEmpty();
    }

    private static async Task<InMemoryOffer> SeededAsync()
    {
        var offer = new InMemoryOffer();
        var now = DateTimeOffset.UtcNow;
        await offer.TrySaveAsync(new FixtureChangedV1("fx-1", "Premier League", "Arsenal", "Chelsea", now.AddHours(2), FixtureStatus.Scheduled, 1,
            [new MarketV1("fx-1-1x2", MarketType.MatchResult, MarketStatus.Open, [new SelectionV1("home", "Arsenal", 2.1m)])], now), 0, CancellationToken.None);
        return offer;
    }
}
