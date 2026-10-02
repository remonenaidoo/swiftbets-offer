using SwiftBets.Offer.Application.Catalog;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Tests;

internal sealed class InMemoryCatalog : ICatalogStore
{
    public bool Fail { get; set; }

    public Dictionary<string, CatalogFixture> Fixtures { get; } = [];

    public Task UpsertAsync(IReadOnlyList<CatalogFixture> fixtures, CancellationToken cancellationToken)
    {
        if (Fail)
        {
            throw new InvalidOperationException("catalogue down");
        }

        foreach (var fixture in fixtures)
        {
            Fixtures[fixture.FixtureId] = fixture;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CatalogSport>> ListSportsAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CatalogSport>>([]);
}
