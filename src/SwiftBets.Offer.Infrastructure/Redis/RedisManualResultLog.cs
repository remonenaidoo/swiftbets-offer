using System.Text.Json;
using StackExchange.Redis;
using SwiftBets.Contracts.Serialization;
using SwiftBets.Contracts.Trading;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Infrastructure.Redis;

public sealed class RedisManualResultLog(IConnectionMultiplexer redis) : IManualResultLog
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    public Task RecordIssuedAsync(ManualResultV1 result, CancellationToken cancellationToken) =>
        redis.GetDatabase().StringSetAsync(OfferKeys.ManualResult(result.ManualResultId), JsonSerializer.Serialize(result, ContractJson.Options), Retention);

    public async Task RecordRejectionAsync(ManualResultRejectedV1 rejection, CancellationToken cancellationToken)
    {
        // One field per coupon, so a redelivered rejection overwrites rather than duplicates.
        var db = redis.GetDatabase();
        var key = OfferKeys.ManualResultRejections(rejection.ManualResultId);
        await db.HashSetAsync(key, rejection.CouponId.ToString(), JsonSerializer.Serialize(
            new ManualResultRejection(rejection.CouponId, rejection.Code, rejection.Message, rejection.RejectedAt), ContractJson.Options));
        await db.KeyExpireAsync(key, Retention);
    }

    public async Task<ManualResultOutcome?> GetAsync(Guid manualResultId, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var issued = await db.StringGetAsync(OfferKeys.ManualResult(manualResultId));
        if (issued.IsNullOrEmpty)
        {
            return null;
        }

        var rejections = await db.HashValuesAsync(OfferKeys.ManualResultRejections(manualResultId));
        return new ManualResultOutcome(
            JsonSerializer.Deserialize<ManualResultV1>(issued.ToString(), ContractJson.Options)!,
            [.. rejections.Select(r => JsonSerializer.Deserialize<ManualResultRejection>(r.ToString(), ContractJson.Options)!).OrderBy(r => r.RejectedAt)]);
    }
}
