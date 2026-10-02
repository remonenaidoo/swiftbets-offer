using Npgsql;
using SwiftBets.BuildingBlocks.Testing;
using SwiftBets.Offer.Application.Catalog;
using SwiftBets.Offer.Infrastructure.Catalog;

[assembly: AssemblyFixture(typeof(PostgresFixture))]

namespace SwiftBets.Offer.Infrastructure.Tests;

public sealed class PostgresCatalogStoreTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_fixture_is_listed_under_its_sport_and_competition_with_its_upcoming_count()
    {
        var store = await StoreAsync();

        await store.UpsertAsync([Fixture("fx-1", 1, "scheduled"), Fixture("fx-2", 1, "finished")], CancellationToken.None);

        var sport = (await store.ListSportsAsync(Now, CancellationToken.None)).ShouldHaveSingleItem();
        (sport.SportId, sport.Competitions.ShouldHaveSingleItem().UpcomingFixtures).ShouldBe(("soccer", 1));
    }

    [Fact]
    public async Task An_older_offer_version_never_overwrites_a_newer_one()
    {
        var store = await StoreAsync();
        await store.UpsertAsync([Fixture("fx-1", 3, "finished")], CancellationToken.None);

        await store.UpsertAsync([Fixture("fx-1", 2, "scheduled")], CancellationToken.None);

        (await store.ListSportsAsync(Now, CancellationToken.None))[0].Competitions[0].UpcomingFixtures.ShouldBe(0);
    }

    private static CatalogFixture Fixture(string id, long version, string status) =>
        new("soccer", "premier-league", "Premier League", id, "Arsenal", "Chelsea", Now.AddHours(2), status, version);

    private async Task<PostgresCatalogStore> StoreAsync()
    {
        var database = "catalog_" + Guid.NewGuid().ToString("N")[..10];
        await using (var admin = new NpgsqlConnection(postgres.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE {database}", admin);
            await create.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Database = database }.ConnectionString;
        var entry = typeof(Program).Assembly.EntryPoint!.Invoke(null, [new[] { $"--ConnectionStrings:SbCatalog={connectionString}" }]);
        (entry is Task<int> task ? await task : (int)entry!).ShouldBe(0);
        return new PostgresCatalogStore(NpgsqlDataSource.Create(connectionString));
    }
}
