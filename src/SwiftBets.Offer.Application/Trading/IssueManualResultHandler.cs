using SwiftBets.Contracts.Errors;
using SwiftBets.Contracts.Results;
using SwiftBets.Contracts.Trading;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Trading;

/// <summary>A trader's result, checked against the offer and published for settlement to apply.</summary>
public sealed class IssueManualResultHandler(IOfferStore store, IOfferEvents events, IManualResultLog log, TimeProvider time)
{
    public sealed record Request(ManualResultScope Scope, ManualResultAction Action, string FixtureId, string? MarketId, Guid? CouponId, string? WinningSelectionId, string Reason, DateTimeOffset? VoidFrom = null);

    public async Task<Result<ManualResultV1>> HandleAsync(Request request, Guid operatorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Error.Validation("reason_required", "Give a reason for the manual result.");
        }

        var now = time.GetUtcNow();
        var timeVoid = request.Action == ManualResultAction.TimeVoid;
        if (timeVoid != request.VoidFrom.HasValue || request.VoidFrom > now)
        {
            return Error.Validation("void_from_invalid", "A time-void names a cut-off in the past; other actions name none.");
        }

        if ((request.Scope == ManualResultScope.Market && string.IsNullOrEmpty(request.MarketId)) || (request.Scope == ManualResultScope.Coupon && request.CouponId is null))
        {
            return Error.Validation("scope_incomplete", "A market result needs a market id; a coupon result needs a coupon id.");
        }

        var fixture = await store.GetAsync(request.FixtureId, cancellationToken);
        if (fixture is null || (request.MarketId is { } marketId && fixture.Markets.All(m => m.MarketId != marketId)))
        {
            return Error.NotFound("market_not_found", "No such fixture or market on offer.");
        }

        var settles = request.Action is ManualResultAction.Settle or ManualResultAction.Override;
        if (settles != (request.WinningSelectionId is not null)
            || (settles && !fixture.Markets.Where(m => request.MarketId is null || m.MarketId == request.MarketId).SelectMany(m => m.Selections).Any(s => s.SelectionId == request.WinningSelectionId)))
        {
            return Error.Validation("winner_invalid", "Settle and override name a winning selection on the market; void names none.");
        }

        var result = new ManualResultV1(Guid.NewGuid(), request.Scope, request.Action, request.FixtureId, request.MarketId, request.CouponId,
            request.WinningSelectionId, request.VoidFrom, request.Reason.Trim(), operatorId, now);
        await log.RecordIssuedAsync(result, cancellationToken);
        await events.ManualResultAsync(result, cancellationToken);
        return Result.Success(result);
    }
}
