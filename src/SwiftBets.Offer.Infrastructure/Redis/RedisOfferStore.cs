using System.Text.Json;
using StackExchange.Redis;
using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Serialization;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Infrastructure.Redis;

public sealed class RedisOfferStore(IConnectionMultiplexer redis) : IOfferStore
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(6);
    private static readonly LuaScript SaveIfVersion = LuaScript.Prepare(ReadScript("SaveIfVersion.lua"));

    public async Task<FixtureChangedV1?> GetAsync(string fixtureId, CancellationToken cancellationToken)
    {
        var json = await redis.GetDatabase().HashGetAsync(OfferKeys.Fixture(fixtureId), "snapshot");
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<FixtureChangedV1>(json.ToString(), ContractJson.Options);
    }

    public async Task<IReadOnlyList<FixtureChangedV1>> ListOpenAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var ids = await db.SortedSetRangeByScoreAsync(OfferKeys.OpenFixtures, now.ToUnixTimeMilliseconds(), double.PositiveInfinity, take: limit);
        var snapshots = await Task.WhenAll(ids.Select(id => db.HashGetAsync(OfferKeys.Fixture(id.ToString()), "snapshot")));
        return [.. snapshots.Where(s => !s.IsNullOrEmpty).Select(s => JsonSerializer.Deserialize<FixtureChangedV1>(s.ToString(), ContractJson.Options)!)];
    }

    public async Task<bool> TrySaveAsync(FixtureChangedV1 snapshot, long expectedVersion, CancellationToken cancellationToken)
    {
        var result = await redis.GetDatabase().ScriptEvaluateAsync(
            SaveIfVersion.ExecutableScript,
            [OfferKeys.Fixture(snapshot.FixtureId), OfferKeys.OpenFixtures],
            [
                expectedVersion,
                snapshot.OfferVersion,
                JsonSerializer.Serialize(snapshot, ContractJson.Options),
                snapshot.Status == FixtureStatus.Scheduled ? "1" : "0",
                snapshot.KickoffAt.ToUnixTimeMilliseconds(),
                (long)Retention.TotalSeconds,
                snapshot.FixtureId,
            ]);
        return (long)result == 1;
    }

    public async Task<bool> IsResultPublishedAsync(string fixtureId, int resultVersion, CancellationToken cancellationToken) =>
        await redis.GetDatabase().KeyExistsAsync(OfferKeys.ResultPublished(fixtureId, resultVersion));

    public Task MarkResultPublishedAsync(string fixtureId, int resultVersion, CancellationToken cancellationToken) =>
        redis.GetDatabase().StringSetAsync(OfferKeys.ResultPublished(fixtureId, resultVersion), "1", Retention);

    private static string ReadScript(string name)
    {
        using var stream = typeof(RedisOfferStore).Assembly.GetManifestResourceStream($"SwiftBets.Offer.Infrastructure.Redis.{name}")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
