using SwiftBets.Contracts.Offer;

namespace SwiftBets.Offer.Application.Ports;

public interface IOfferEvents
{
    Task FixtureChangedAsync(FixtureChangedV1 snapshot, CancellationToken cancellationToken);

    Task ResultPublishedAsync(ResultPublishedV1 result, CancellationToken cancellationToken);
}
