namespace SwiftBets.Offer.Domain.Tests;

public sealed class ReplayTimelineTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly HistoricalPrices Prices = new(2m, 3.4m, 4m, 1.9m, 1.9m);

    [Fact]
    public void Fixture_is_open_before_kickoff_and_finished_after_its_result_slot()
    {
        var slot = Timeline().ActiveAt(Epoch.AddSeconds(15), TimeSpan.Zero).Single(s => s.Match.Index == 1);

        slot.PhaseAt(Epoch.AddSeconds(15)).ShouldBe(FixturePhase.Open);
        slot.PhaseAt(slot.ResultAt).ShouldBe(FixturePhase.Finished);
    }

    [Fact]
    public void Fixture_is_not_listed_before_its_lead_time() =>
        Timeline().ActiveAt(Epoch.AddSeconds(1), TimeSpan.Zero).ShouldNotContain(s => s.Match.Index == 2);

    private static ReplayTimeline Timeline() =>
        new("s", [.. Enumerable.Range(0, 3).Select(i => new HistoricalMatch(i, "H", "A", 1, 0, Prices, Prices))], Epoch, TimeSpan.FromSeconds(10), listLeadSlots: 2);
}
