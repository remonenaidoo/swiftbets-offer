using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Trading;

namespace SwiftBets.Offer.Application.Ports;

public interface IOfferEvents
{
    Task FixtureChangedAsync(FixtureChangedV1 snapshot, CancellationToken cancellationToken);

    Task ResultPublishedAsync(ResultPublishedV1 result, CancellationToken cancellationToken);

    Task ManualResultAsync(ManualResultV1 result, CancellationToken cancellationToken);

    Task MarketStatusChangedAsync(MarketStatusChangedV1 change, CancellationToken cancellationToken);
}
