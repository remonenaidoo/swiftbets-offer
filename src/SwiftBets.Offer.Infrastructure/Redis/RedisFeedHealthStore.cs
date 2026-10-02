using StackExchange.Redis;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Infrastructure.Redis;

public sealed class RedisFeedHealthStore(IConnectionMultiplexer redis) : IFeedHealthStore
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(6);

    public Task RecordSeenAsync(string fixtureId, DateTimeOffset updatedAt, CancellationToken cancellationToken) =>
        redis.GetDatabase().StringSetAsync(OfferKeys.FeedSeen(fixtureId), updatedAt.ToUnixTimeMilliseconds(), Retention);

    public async Task<DateTimeOffset?> GetSeenAsync(string fixtureId, CancellationToken cancellationToken) =>
        await redis.GetDatabase().StringGetAsync(OfferKeys.FeedSeen(fixtureId)) is { IsNullOrEmpty: false } value
            ? DateTimeOffset.FromUnixTimeMilliseconds((long)value)
            : null;

    public async Task<IReadOnlyList<string>?> GetStaleSuspendedAsync(string fixtureId, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        return await db.KeyExistsAsync(OfferKeys.StaleSuspended(fixtureId))
            ? [.. (await db.SetMembersAsync(OfferKeys.StaleSuspended(fixtureId))).Select(v => v.ToString()).Where(v => v.Length > 0)]
            : null;
    }

    public async Task SetStaleSuspendedAsync(string fixtureId, IReadOnlyList<string> marketIds, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        // An empty marker member keeps the key present even when there was nothing to suspend.
        await db.SetAddAsync(OfferKeys.StaleSuspended(fixtureId), [string.Empty, .. marketIds.Select(m => (RedisValue)m)]);
        await db.KeyExpireAsync(OfferKeys.StaleSuspended(fixtureId), Retention);
    }

    public Task ClearStaleSuspendedAsync(string fixtureId, CancellationToken cancellationToken) =>
        redis.GetDatabase().KeyDeleteAsync(OfferKeys.StaleSuspended(fixtureId));
}
