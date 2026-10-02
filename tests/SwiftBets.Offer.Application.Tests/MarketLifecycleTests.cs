using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Markets;

namespace SwiftBets.Offer.Application.Tests;

public sealed class MarketLifecycleTests
{
    [Fact]
    public void An_open_market_can_be_suspended_and_reopened() =>
        (MarketLifecycle.CanMove(MarketStatus.Open, MarketStatus.Suspended), MarketLifecycle.CanMove(MarketStatus.Suspended, MarketStatus.Open)).ShouldBe((true, true));

    [Fact]
    public void A_closed_market_never_reopens_whoever_asks() =>
        (MarketLifecycle.Next(MarketStatus.Closed, MarketStatus.Open), MarketLifecycle.Next(MarketStatus.Closed, MarketStatus.Suspended)).ShouldBe((MarketStatus.Closed, MarketStatus.Closed));
}
