using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftBets.BuildingBlocks.Core;
using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.BuildingBlocks.Redis;
using SwiftBets.Offer.Application.Ports;
using SwiftBets.Offer.Application.Replay;
using SwiftBets.Offer.Infrastructure.Messaging;
using SwiftBets.Offer.Infrastructure.Redis;
using SwiftBets.Offer.Infrastructure.Replay;
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
        if (configuration.GetValue("Replay:Enabled", true))
        {
            services.AddHostedService<ReplayWorker>();
        }

        return services;
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value ? value : throw new InvalidOperationException($"Configuration '{key}' is required.");
}
