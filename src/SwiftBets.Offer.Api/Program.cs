using System.Text.Json;
using System.Text.Json.Serialization;
using SwiftBets.BuildingBlocks.Observability;
using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Offer.Api.Endpoints;
using SwiftBets.Offer.Application;
using SwiftBets.Offer.Infrastructure;

if (HealthProbe.TryRun(args) is { } probeExitCode)
{
    return probeExitCode;
}

var builder = WebApplication.CreateBuilder(args);
builder.AddSwiftBetsObservability("swiftbets-offer");
builder.Services.AddSwiftBetsWeb();
// Request bodies use the contract enum names ("market", "void"), as responses do.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddSwiftBetsJwtBearer(builder.Configuration);
builder.Services.AddOfferApplication();
builder.Services.AddOfferInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseSwiftBetsObservability();
app.UseSwiftBetsWeb();
app.UseAuthentication();
app.UseAuthorization();
app.MapSwiftBetsOperationalEndpoints();
app.MapOfferEndpoints();

await app.RunAsync();
return 0;

public partial class Program;
