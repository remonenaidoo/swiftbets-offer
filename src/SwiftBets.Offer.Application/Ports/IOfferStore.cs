using SwiftBets.Contracts.Offer;

namespace SwiftBets.Offer.Application.Ports;

public interface IOfferStore
{
    Task<FixtureChangedV1?> GetAsync(string fixtureId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FixtureChangedV1>> ListOpenAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);

    /// <summary>Writes the snapshot only if the stored version is <paramref name="expectedVersion"/> (0 for a new fixture).</summary>
    Task<bool> TrySaveAsync(FixtureChangedV1 snapshot, long expectedVersion, CancellationToken cancellationToken);

    Task<bool> IsResultPublishedAsync(string fixtureId, int resultVersion, CancellationToken cancellationToken);

    Task MarkResultPublishedAsync(string fixtureId, int resultVersion, CancellationToken cancellationToken);
}
