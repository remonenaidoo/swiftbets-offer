using SwiftBets.Contracts.Trading;

namespace SwiftBets.Offer.Application.Ports;

/// <summary>What became of each manual result a trader issued: the coupons settlement refused to change, and why.</summary>
public interface IManualResultLog
{
    Task RecordIssuedAsync(ManualResultV1 result, CancellationToken cancellationToken);

    Task RecordRejectionAsync(ManualResultRejectedV1 rejection, CancellationToken cancellationToken);

    Task<ManualResultOutcome?> GetAsync(Guid manualResultId, CancellationToken cancellationToken);
}

public sealed record ManualResultRejection(Guid CouponId, string Code, string Message, DateTimeOffset RejectedAt);

public sealed record ManualResultOutcome(ManualResultV1 Result, IReadOnlyList<ManualResultRejection> Rejections);
