using SwiftBets.Offer.Domain;

namespace SwiftBets.Offer.Application.Ports;

public interface ISeasonSource
{
    string Competition { get; }

    string Season { get; }

    IReadOnlyList<HistoricalMatch> Matches { get; }
}
