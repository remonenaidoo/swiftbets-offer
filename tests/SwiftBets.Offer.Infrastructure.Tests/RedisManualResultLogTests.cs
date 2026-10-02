using StackExchange.Redis;
using SwiftBets.BuildingBlocks.Testing;
using SwiftBets.Contracts.Trading;
using SwiftBets.Offer.Infrastructure.Redis;

namespace SwiftBets.Offer.Infrastructure.Tests;

public sealed class RedisManualResultLogTests(RedisFixture redis)
{
    [Fact]
    public async Task A_rejection_is_recorded_once_against_the_result_that_caused_it()
    {
        var log = new RedisManualResultLog(await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString));
        var issued = new ManualResultV1(Guid.NewGuid(), ManualResultScope.Market, ManualResultAction.Void, "fx-1", "fx-1-1x2", null, null, null, "abandoned", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var coupon = Guid.NewGuid();
        await log.RecordIssuedAsync(issued, CancellationToken.None);

        var rejection = new ManualResultRejectedV1(issued.ManualResultId, coupon, ManualResultRejectedV1.CouponCashedOut, "cashed out", DateTimeOffset.UtcNow);
        await log.RecordRejectionAsync(rejection, CancellationToken.None);
        await log.RecordRejectionAsync(rejection, CancellationToken.None);

        var outcome = (await log.GetAsync(issued.ManualResultId, CancellationToken.None)).ShouldNotBeNull();
        (outcome.Rejections.ShouldHaveSingleItem().CouponId, outcome.Rejections[0].Code).ShouldBe((coupon, "coupon_cashed_out"));
    }

    [Fact]
    public async Task An_unknown_result_has_no_outcome()
    {
        var log = new RedisManualResultLog(await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString));

        (await log.GetAsync(Guid.NewGuid(), CancellationToken.None)).ShouldBeNull();
    }
}
