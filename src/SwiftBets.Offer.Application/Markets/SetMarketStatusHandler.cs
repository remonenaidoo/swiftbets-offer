using SwiftBets.Contracts.Errors;
using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Results;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Markets;

/// <summary>Operator suspension or resumption of one market; bumps the offer version so in-flight bets priced earlier are refused.</summary>
public sealed class SetMarketStatusHandler(IOfferStore store, IOfferEvents events, TimeProvider time)
{
    private const int MaxAttempts = 3;

    public async Task<Result<FixtureChangedV1>> HandleAsync(string fixtureId, string marketId, bool suspend, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var current = await store.GetAsync(fixtureId, cancellationToken);
            var market = current?.Markets.FirstOrDefault(m => m.MarketId == marketId);
            if (current is null || market is null)
            {
                return Error.NotFound("market_not_found", $"Market {marketId} is not on fixture {fixtureId}.");
            }

            if (market.Status == MarketStatus.Closed || (!suspend && current.Status != FixtureStatus.Scheduled))
            {
                return Error.Conflict("market_not_tradable", "The market has closed.");
            }

            var target = suspend ? MarketStatus.Suspended : MarketStatus.Open;
            if (market.Status == target)
            {
                return Result.Success(current);
            }

            var next = current with
            {
                OfferVersion = current.OfferVersion + 1,
                ChangedAt = time.GetUtcNow(),
                Markets = [.. current.Markets.Select(m => m.MarketId == marketId ? m with { Status = target } : m)],
            };
            if (await store.TrySaveAsync(next, current.OfferVersion, cancellationToken))
            {
                await events.FixtureChangedAsync(next, cancellationToken);
                return Result.Success(next);
            }
        }

        return Error.Conflict("offer_contended", "The offer changed concurrently; retry.");
    }
}
