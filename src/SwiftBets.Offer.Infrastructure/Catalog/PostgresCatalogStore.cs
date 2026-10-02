using Dapper;
using Npgsql;
using SwiftBets.BuildingBlocks.Persistence;
using SwiftBets.Offer.Application.Catalog;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Infrastructure.Catalog;

public sealed class PostgresCatalogStore(NpgsqlDataSource dataSource) : ICatalogStore
{
    private static readonly SqlResources Sql = SqlResources.For<PostgresCatalogStore>();

    public async Task UpsertAsync(IReadOnlyList<CatalogFixture> fixtures, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(Sql.Get("Catalog.Upsert"), new
        {
            SportIds = fixtures.Select(f => f.SportId).ToArray(),
            CompetitionIds = fixtures.Select(f => f.CompetitionId).ToArray(),
            CompetitionNames = fixtures.Select(f => f.CompetitionName).ToArray(),
            FixtureIds = fixtures.Select(f => f.FixtureId).ToArray(),
            HomeTeams = fixtures.Select(f => f.HomeTeam).ToArray(),
            AwayTeams = fixtures.Select(f => f.AwayTeam).ToArray(),
            KickoffTimes = fixtures.Select(f => f.KickoffAt.UtcDateTime).ToArray(),
            Statuses = fixtures.Select(f => f.Status).ToArray(),
            OfferVersions = fixtures.Select(f => f.OfferVersion).ToArray(),
        }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<CatalogSport>> ListSportsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<Row>(new CommandDefinition(Sql.Get("Catalog.Sports"), new { Now = now.UtcDateTime }, cancellationToken: cancellationToken));
        return [.. rows.GroupBy(r => (r.SportId, r.SportName)).Select(g => new CatalogSport(g.Key.SportId, g.Key.SportName,
            [.. g.Select(r => new CatalogCompetition(r.CompetitionId, r.CompetitionName, r.UpcomingFixtures))]))];
    }

    private sealed record Row(string SportId, string SportName, string CompetitionId, string CompetitionName, int UpcomingFixtures);
}
