using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SwiftBets.Contracts.Trading;
using SwiftBets.Offer.Application.Trading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SwiftBets.BuildingBlocks.Testing;

namespace SwiftBets.Offer.Api.Tests;

public sealed class HostTests : IClassFixture<HostTests.Factory>
{
    private readonly HttpClient _client;
    private readonly Factory _factory;

    public HostTests(Factory factory) => (_factory, _client) = (factory, factory.CreateClient());

    [Fact]
    public void A_manual_result_request_binds_from_contract_enum_names()
    {
        var options = _factory.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;

        var request = JsonSerializer.Deserialize<IssueManualResultHandler.Request>(
            "{\"scope\":\"market\",\"action\":\"void\",\"fixtureId\":\"f\",\"marketId\":\"m\",\"reason\":\"r\"}", options)!;

        (request.Scope, request.Action).ShouldBe((ManualResultScope.Market, ManualResultAction.Void));
    }

    [Fact]
    public async Task The_result_drill_does_not_exist_while_fault_injection_is_off()
    {
        using var response = await _client.PostAsync(new Uri("/admin/trading/drills/results", UriKind.Relative), null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Liveness_is_healthy_without_dependencies()
    {
        using var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Readiness_reports_unavailable_dependencies()
    {
        using var response = await _client.GetAsync(new Uri("/health/ready", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Unknown_route_returns_the_error_envelope()
    {
        using var response = await _client.GetAsync(new Uri("/no-such-route", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Suspending_a_market_requires_an_operator_token()
    {
        using var response = await _client.PostAsync(new Uri("/fixtures/fx/markets/fx-1x2/suspend", UriKind.Relative), null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Metrics_are_exposed()
    {
        var body = await _client.GetStringAsync(new Uri("/metrics", UriKind.Relative), TestContext.Current.CancellationToken);

        body.ShouldContain("process_cpu_seconds_total");
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
        builder.UseSetting("Kafka:BootstrapServers", "127.0.0.1:1");
        builder.UseSetting("ConnectionStrings:Redis", "127.0.0.1:1,connectTimeout=200");
        builder.UseSetting("ConnectionStrings:SbCatalog", "Host=127.0.0.1;Port=1;Database=sb_catalog;Username=x;Password=x;Timeout=1");
            builder.UseSetting("Replay:Enabled", "false");
            builder.UseSetting("Jwt:Authority", TestJwt.Issuer);
            builder.ConfigureServices(services => services.UseTestJwt());
        }
    }
}
