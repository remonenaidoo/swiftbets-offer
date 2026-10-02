using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Contracts.Errors;
using SwiftBets.Contracts.Serialization;
using SwiftBets.Offer.Application.Markets;
using SwiftBets.Offer.Application.Queries;
using SwiftBets.Offer.Application.Trading;

namespace SwiftBets.Offer.Api.Endpoints;

public static class OfferEndpoints
{
    public static IEndpointRouteBuilder MapOfferEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var fixtures = endpoints.MapGroup("/fixtures");

        fixtures.MapGet("/", async (OfferQueries queries, int? limit, CancellationToken cancellationToken) =>
            Results.Json(await queries.ListOpenAsync(limit ?? 50, cancellationToken), ContractJson.Options));

        fixtures.MapGet("/{fixtureId}", async (string fixtureId, OfferQueries queries, HttpContext context, CancellationToken cancellationToken) =>
            await queries.GetAsync(fixtureId, cancellationToken) is { } fixture
                ? Results.Json(fixture, ContractJson.Options)
                : Error.NotFound("fixture_not_found", $"Fixture {fixtureId} is not on offer.").ToHttpResult(context));

        fixtures.MapPost("/{fixtureId}/markets/{marketId}/suspend", (string fixtureId, string marketId, SetMarketStatusHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            SetStatusAsync(handler, fixtureId, marketId, suspend: true, context, cancellationToken))
            .RequireAuthorization(Roles.Operator);

        fixtures.MapPost("/{fixtureId}/markets/{marketId}/resume", (string fixtureId, string marketId, SetMarketStatusHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            SetStatusAsync(handler, fixtureId, marketId, suspend: false, context, cancellationToken))
            .RequireAuthorization(Roles.Operator);

        endpoints.MapPost("/admin/trading/manual-results", async (IssueManualResultHandler.Request request, IssueManualResultHandler handler, HttpContext context, CancellationToken cancellationToken) =>
        {
            var operatorId = Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
            var result = await handler.HandleAsync(request, operatorId, cancellationToken);
            return result.IsSuccess ? Results.Json(result.Value, ContractJson.Options, statusCode: StatusCodes.Status202Accepted) : result.ToHttpResult(context);
        })
        .RequireAuthorization(Roles.Operator);

        return endpoints;
    }

    private static async Task<IResult> SetStatusAsync(SetMarketStatusHandler handler, string fixtureId, string marketId, bool suspend, HttpContext context, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(fixtureId, marketId, suspend, cancellationToken)).ToHttpResult(context);
}
