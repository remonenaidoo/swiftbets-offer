using StackExchange.Redis;
using SwiftBets.BuildingBlocks.Testing;
using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Infrastructure.Redis;

[assembly: AssemblyFixture(typeof(RedisFixture))]

namespace SwiftBets.Offer.Infrastructure.Tests;

public sealed class RedisOfferStoreTests(RedisFixture redis)
{
    [Fact]
    public async Task Saved_open_fixture_is_readable_and_listed()
    {
        var store = await StoreAsync();
        var fixture = Fixture("fx-a", version: 1);

        (await store.TrySaveAsync(fixture, 0, CancellationToken.None)).ShouldBeTrue();

        (await store.GetAsync("fx-a", CancellationToken.None))!.OfferVersion.ShouldBe(1);
        (await store.ListOpenAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None)).ShouldContain(f => f.FixtureId == "fx-a");
    }

    [Fact]
    public async Task Save_against_a_stale_version_is_refused()
    {
        var store = await StoreAsync();
        await store.TrySaveAsync(Fixture("fx-b", version: 1), 0, CancellationToken.None);
        await store.TrySaveAsync(Fixture("fx-b", version: 2), 1, CancellationToken.None);

        (await store.TrySaveAsync(Fixture("fx-b", version: 2), 1, CancellationToken.None)).ShouldBeFalse();
    }

    private async Task<RedisOfferStore> StoreAsync() => new(await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString));

    private static FixtureChangedV1 Fixture(string id, long version) =>
        new(id, "PL", "Arsenal", "Chelsea", DateTimeOffset.UtcNow.AddHours(1), FixtureStatus.Scheduled, version,
            [new MarketV1($"{id}-1x2", MarketType.MatchResult, MarketStatus.Open, [new SelectionV1("home", "Arsenal", 2m)])], DateTimeOffset.UtcNow);
}
