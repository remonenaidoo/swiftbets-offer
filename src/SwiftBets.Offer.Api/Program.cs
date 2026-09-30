using SwiftBets.BuildingBlocks.Observability;
using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Offer.Application;
using SwiftBets.Offer.Infrastructure;

if (HealthProbe.TryRun(args) is { } probeExitCode)
{
    return probeExitCode;
}

var builder = WebApplication.CreateBuilder(args);
builder.AddSwiftBetsObservability("swiftbets-offer");
builder.Services.AddSwiftBetsWeb();
builder.Services.AddOfferApplication();
builder.Services.AddOfferInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseSwiftBetsObservability();
app.UseSwiftBetsWeb();
app.MapSwiftBetsOperationalEndpoints();
app.MapGet("/", () => Results.Ok(new { service = "swiftbets-offer" })).ExcludeFromDescription();

await app.RunAsync();
return 0;

public partial class Program;
