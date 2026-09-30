namespace SwiftBets.Offer.Domain;

public sealed class ReplayTimeline
{
    private readonly IReadOnlyList<HistoricalMatch> _matches;
    private readonly DateTimeOffset _epoch;
    private readonly TimeSpan _slot;
    private readonly int _listLeadSlots;
    private readonly string _season;

    public ReplayTimeline(string season, IReadOnlyList<HistoricalMatch> matches, DateTimeOffset epoch, TimeSpan slot, int listLeadSlots)
    {
        ArgumentOutOfRangeException.ThrowIfZero(matches.Count);
        ArgumentOutOfRangeException.ThrowIfLessThan(listLeadSlots, 1);
        _season = season;
        _matches = matches;
        _epoch = epoch;
        _slot = slot;
        _listLeadSlots = listLeadSlots;
    }

    public TimeSpan CycleLength => _slot * (_matches.Count + 1);

    /// <summary>Every slot that is listed, open or awaiting its result at <paramref name="now"/>, plus those finished in the last <paramref name="lookBack"/>.</summary>
    public IEnumerable<ReplaySlot> ActiveAt(DateTimeOffset now, TimeSpan lookBack)
    {
        var cycle = (long)Math.Floor((now - _epoch) / CycleLength);
        for (var c = Math.Max(0, cycle - 1); c <= cycle + 1; c++)
        {
            var cycleStart = _epoch + (CycleLength * c);
            foreach (var match in _matches)
            {
                var kickoff = cycleStart + (_slot * (match.Index + 1));
                var slot = new ReplaySlot($"{_season}-c{c}-m{match.Index:D3}", match, kickoff - (_slot * _listLeadSlots), kickoff, kickoff + _slot);
                if (slot.ListedAt <= now && slot.ResultAt + lookBack >= now)
                {
                    yield return slot;
                }
            }
        }
    }
}
