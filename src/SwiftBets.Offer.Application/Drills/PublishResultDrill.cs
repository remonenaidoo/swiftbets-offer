using SwiftBets.Contracts.Errors;
using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Results;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Drills;

/// <summary>
/// Publishes a feed result at a chosen version, so drills can replay corrections and out-of-order delivery through the
/// whole stack. Only reachable where fault injection is enabled, which production refuses.
/// </summary>
public sealed class PublishResultDrill(IOfferStore store, IOfferEvents events, TimeProvider time)
{
    public sealed record Request(string FixtureId, int Version, ResultStatus Status, int HomeGoals, int AwayGoals);

    public async Task<Result<ResultPublishedV1>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (request.Version is < 1 or > 999 || request.HomeGoals is < 0 or > 50 || request.AwayGoals is < 0 or > 50)
        {
            return Error.Validation("drill_invalid", "Version is 1 to 999 and goals 0 to 50.");
        }

        if (await store.GetAsync(request.FixtureId, cancellationToken) is null)
        {
            return Error.NotFound("fixture_not_found", $"Fixture {request.FixtureId} is not on offer.");
        }

        var result = new ResultPublishedV1(request.FixtureId, request.Version, request.Status, request.HomeGoals, request.AwayGoals, time.GetUtcNow());
        await events.ResultPublishedAsync(result, cancellationToken);
        await store.MarkResultPublishedAsync(request.FixtureId, request.Version, cancellationToken);
        return Result<ResultPublishedV1>.Success(result);
    }
}
