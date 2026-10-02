using SwiftBets.Offer.Application.Catalog;

namespace SwiftBets.Offer.Application.Ports;

/// <summary>The Postgres catalogue: sports, competitions and fixtures for browsing. Prices stay in the offer store.</summary>
public interface ICatalogStore
{
    /// <summary>Idempotent: a fixture row only moves forward in offer version.</summary>
    Task UpsertAsync(IReadOnlyList<CatalogFixture> fixtures, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogSport>> ListSportsAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
