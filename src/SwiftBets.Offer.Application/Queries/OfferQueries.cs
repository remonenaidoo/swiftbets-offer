using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Application.Catalog;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Queries;

public sealed class OfferQueries(IOfferStore store, ICatalogStore catalog, TimeProvider time)
{
    public Task<IReadOnlyList<FixtureChangedV1>> ListOpenAsync(int limit, CancellationToken cancellationToken) =>
        store.ListOpenAsync(time.GetUtcNow(), Math.Clamp(limit, 1, 200), cancellationToken);

    public Task<IReadOnlyList<CatalogSport>> ListSportsAsync(CancellationToken cancellationToken) =>
        catalog.ListSportsAsync(time.GetUtcNow(), cancellationToken);

    public Task<FixtureChangedV1?> GetAsync(string fixtureId, CancellationToken cancellationToken) =>
        store.GetAsync(fixtureId, cancellationToken);
}
