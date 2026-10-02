using Microsoft.Extensions.DependencyInjection;
using SwiftBets.Offer.Application.Feed;
using SwiftBets.Offer.Application.Markets;
using SwiftBets.Offer.Application.Queries;

namespace SwiftBets.Offer.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddOfferApplication(this IServiceCollection services)
    {
        services.AddSingleton<FeedSync>();
        services.AddSingleton<StalenessGuard>();
        services.AddSingleton<SetMarketStatusHandler>();
        services.AddSingleton<Trading.IssueManualResultHandler>();
        services.AddSingleton<OfferQueries>();
        return services;
    }
}
