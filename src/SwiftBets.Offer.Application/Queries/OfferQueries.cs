using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Queries;

public sealed class OfferQueries(IOfferStore store, TimeProvider time)
{
    public Task<IReadOnlyList<FixtureChangedV1>> ListOpenAsync(int limit, CancellationToken cancellationToken) =>
        store.ListOpenAsync(time.GetUtcNow(), Math.Clamp(limit, 1, 200), cancellationToken);

    public Task<FixtureChangedV1?> GetAsync(string fixtureId, CancellationToken cancellationToken) =>
        store.GetAsync(fixtureId, cancellationToken);
}
