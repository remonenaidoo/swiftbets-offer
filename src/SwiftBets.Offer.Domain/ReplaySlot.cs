namespace SwiftBets.Offer.Domain;

/// <summary>
/// Where one historical match sits on the compressed replay timeline. A season of N matches plays in one cycle; match i
/// kicks off at slot i+1, is listed <c>listLead</c> slots before kickoff and gets its result one slot after.
/// </summary>
public sealed record ReplaySlot(string FixtureId, HistoricalMatch Match, DateTimeOffset ListedAt, DateTimeOffset KickoffAt, DateTimeOffset ResultAt)
{
    public FixturePhase PhaseAt(DateTimeOffset now) =>
        now < ListedAt ? FixturePhase.NotYetListed
        : now < KickoffAt ? FixturePhase.Open
        : now < ResultAt ? FixturePhase.AwaitingResult
        : FixturePhase.Finished;

    public double PriceProgressAt(DateTimeOffset now) =>
        (now - ListedAt).TotalMilliseconds / (KickoffAt - ListedAt).TotalMilliseconds;
}
