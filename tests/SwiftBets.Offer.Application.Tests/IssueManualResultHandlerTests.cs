using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Trading;
using SwiftBets.Offer.Application.Trading;

namespace SwiftBets.Offer.Application.Tests;

public sealed class IssueManualResultHandlerTests
{
    private static readonly Guid Operator = Guid.NewGuid();

    [Fact]
    public async Task A_market_void_is_published_with_the_operator_and_reason()
    {
        var offer = await SeededAsync();

        var result = await Handler(offer).HandleAsync(new(ManualResultScope.Market, ManualResultAction.Void, "fx", "fx-1x2", null, null, " abandoned "), Operator, CancellationToken.None);

        result.Value.OperatorId.ShouldBe(Operator);
        result.Value.Reason.ShouldBe("abandoned");
        offer.ManualResults.ShouldHaveSingleItem().ShouldBe(result.Value);
    }

    [Theory]
    [InlineData(ManualResultScope.Market, ManualResultAction.Settle, "fx-1x2", "away", "winner_invalid")]
    [InlineData(ManualResultScope.Market, ManualResultAction.Void, "fx-1x2", "home", "winner_invalid")]
    [InlineData(ManualResultScope.Market, ManualResultAction.Void, null, null, "scope_incomplete")]
    [InlineData(ManualResultScope.Market, ManualResultAction.TimeVoid, "fx-1x2", null, "void_from_invalid")]
    [InlineData(ManualResultScope.Market, ManualResultAction.Void, "nope", null, "market_not_found")]
    public async Task Invalid_requests_are_refused_and_nothing_is_published(ManualResultScope scope, ManualResultAction action, string? market, string? winner, string code)
    {
        var offer = await SeededAsync();

        var result = await Handler(offer).HandleAsync(new(scope, action, "fx", market, null, winner, "reason"), Operator, CancellationToken.None);

        result.Error!.Code.ShouldBe(code);
        offer.ManualResults.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_time_void_carries_its_cut_off_to_settlement()
    {
        var offer = await SeededAsync();
        var cutOff = DateTimeOffset.UtcNow.AddMinutes(-5);

        var result = await Handler(offer).HandleAsync(new(ManualResultScope.Market, ManualResultAction.TimeVoid, "fx", "fx-1x2", null, null, "late bets after a goal", cutOff), Operator, CancellationToken.None);

        result.Value.VoidFrom.ShouldBe(cutOff);
        offer.ManualResults.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_settle_naming_a_selection_on_the_market_is_published()
    {
        var offer = await SeededAsync();

        var result = await Handler(offer).HandleAsync(new(ManualResultScope.Fixture, ManualResultAction.Settle, "fx", null, null, "home", "feed outage"), Operator, CancellationToken.None);

        result.Value.WinningSelectionId.ShouldBe("home");
        result.Value.Scope.ShouldBe(ManualResultScope.Fixture);
    }

    private static IssueManualResultHandler Handler(InMemoryOffer offer) => new(offer, offer, TimeProvider.System);

    private static async Task<InMemoryOffer> SeededAsync()
    {
        var offer = new InMemoryOffer();
        var market = new MarketV1("fx-1x2", MarketType.MatchResult, MarketStatus.Open, [new SelectionV1("home", "Arsenal", 2m)]);
        await offer.TrySaveAsync(new FixtureChangedV1("fx", "PL", "Arsenal", "Chelsea", DateTimeOffset.UtcNow.AddHours(1), FixtureStatus.Scheduled, 1, [market], DateTimeOffset.UtcNow), 0, CancellationToken.None);
        return offer;
    }
}
