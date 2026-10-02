using Microsoft.Extensions.DependencyInjection;
using SwiftBets.Offer.Application.Markets;
using SwiftBets.Offer.Application.Queries;
using SwiftBets.Offer.Application.Replay;

namespace SwiftBets.Offer.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddOfferApplication(this IServiceCollection services)
    {
        services.AddSingleton<ReplayEngine>();
        services.AddSingleton<SetMarketStatusHandler>();
        services.AddSingleton<Trading.IssueManualResultHandler>();
        services.AddSingleton<OfferQueries>();
        return services;
    }
}
