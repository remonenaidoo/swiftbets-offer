using SwiftBets.Contracts.Offer;

namespace SwiftBets.Offer.Application.Markets;

/// <summary>A market moves between open and suspended any number of times; closed is final, whoever asks.</summary>
public static class MarketLifecycle
{
    public static bool CanMove(MarketStatus from, MarketStatus to) => from != MarketStatus.Closed || to == MarketStatus.Closed;

    /// <summary>The status to store when a source asks for <paramref name="requested"/>: a closed market stays closed.</summary>
    public static MarketStatus Next(MarketStatus current, MarketStatus requested) => CanMove(current, requested) ? requested : current;
}
