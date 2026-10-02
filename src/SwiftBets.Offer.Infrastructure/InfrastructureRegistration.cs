using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using SwiftBets.BuildingBlocks.Core;
using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.BuildingBlocks.Redis;
using SwiftBets.BuildingBlocks.Resilience;
using SwiftBets.Contracts.Messaging;
using SwiftBets.Contracts.Trading;
using SwiftBets.Offer.Application.Feed;
using SwiftBets.Offer.Application.Ports;
using SwiftBets.Offer.Application.Replay;
using SwiftBets.Offer.Infrastructure.ApiFootball;
using SwiftBets.Offer.Infrastructure.Catalog;
using SwiftBets.Offer.Infrastructure.Feed;
using SwiftBets.Offer.Infrastructure.Messaging;
using SwiftBets.Offer.Infrastructure.Redis;
using SwiftBets.Offer.Infrastructure.Season;

namespace SwiftBets.Offer.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddOfferInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKafkaMessaging(configuration);
        services.AddSwiftBetsRedis(Required(configuration, "ConnectionStrings:Redis"));
        services.AddFaultInjection(configuration);
        services.AddValidatedOptions<ReplayOptions>(configuration, ReplayOptions.SectionName);
        services.AddSingleton<ISeasonSource, EmbeddedSeasonSource>();
        services.AddSingleton<IOfferStore, RedisOfferStore>();
        services.AddSingleton<IOfferEvents, KafkaOfferEvents>();
        services.AddSingleton<IManualResultLog, RedisManualResultLog>();
        services.AddKafkaConsumer<ManualResultRejectedV1, ManualResultRejectedConsumer>(Topics.ManualResultRejected, "swiftbets.offer.manual-result-rejected");
        services.AddValidatedOptions<FeedOptions>(configuration, FeedOptions.SectionName);
        services.AddValidatedOptions<ApiFootballOptions>(configuration, ApiFootballOptions.SectionName);
        var apiFootball = configuration.GetValue("ApiFootball:Enabled", false);
        if (apiFootball)
        {
            Required(configuration, "ApiFootball:ApiKey");
            services.AddHttpClient(ApiFootballFeedAdapter.ClientName, (sp, http) =>
                    http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<ApiFootballOptions>>().Value.BaseAddress.TrimEnd('/') + "/"))
                .AddIdempotentResilience();
            services.AddSingleton<IFeedAdapter, ApiFootballFeedAdapter>();
        }
        else
        {
            services.AddSingleton<IFeedAdapter, ReplayFeedAdapter>();
        }
        services.AddSingleton<IFeedHealthStore, RedisFeedHealthStore>();
        var catalog = Required(configuration, "ConnectionStrings:SbCatalog");
        services.AddKeyedSingleton("catalog", (_, _) => NpgsqlDataSource.Create(catalog));
        services.AddSingleton<ICatalogStore>(sp => new PostgresCatalogStore(sp.GetRequiredKeyedService<NpgsqlDataSource>("catalog")));
        if (apiFootball || configuration.GetValue("Replay:Enabled", true))
        {
            services.AddHostedService<FeedWorker>();
        }

        return services;
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value ? value : throw new InvalidOperationException($"Configuration '{key}' is required.");
}
