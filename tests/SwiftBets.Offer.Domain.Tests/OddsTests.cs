namespace SwiftBets.Offer.Domain.Tests;

public sealed class OddsTests
{
    [Fact]
    public void Odds_are_truncated_to_two_places() => Odds.From(2.349m).Value.ShouldBe(2.34m);

    [Fact]
    public void Odds_below_the_minimum_are_rejected() => Should.Throw<ArgumentOutOfRangeException>(() => Odds.From(1.001m));
}
