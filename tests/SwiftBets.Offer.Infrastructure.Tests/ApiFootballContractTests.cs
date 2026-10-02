using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SwiftBets.Contracts.Offer;
using SwiftBets.Offer.Infrastructure.ApiFootball;

namespace SwiftBets.Offer.Infrastructure.Tests;

public sealed class ApiFootballContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Recorded_responses_map_to_the_same_markets_the_replay_offers_and_a_result()
    {
        var (adapter, handler) = Build(path => path.StartsWith("fixtures", StringComparison.Ordinal) ? "fixtures.json"
            : path.Contains("bet=1", StringComparison.Ordinal) ? "odds-match-winner.json" : "odds-goals-over-under.json");

        var poll = await adapter.PollAsync(Now, CancellationToken.None);

        var listed = poll.Fixtures.Single(f => f.FixtureId == "af-1300001");
        (listed.Competition, listed.Status, listed.UpdatedAt).ShouldBe(("Premier League", FixtureStatus.Scheduled, Now.AddMinutes(-115)));
        listed.Markets.Select(m => m.MarketId).ShouldBe(["af-1300001-1x2", "af-1300001-ou25"]);
        listed.Markets[0].Selections.Select(s => (s.SelectionId, s.Odds)).ShouldBe([("home", 1.85m), ("draw", 3.40m), ("away", 4.20m)]);
        listed.Markets[1].Selections.Select(s => (s.SelectionId, s.Odds)).ShouldBe([("over", 1.90m), ("under", 1.85m)]);
        poll.Results.ShouldHaveSingleItem().ShouldBe(new Application.Feed.FeedResult("af-1300002", 1, ResultStatus.Official, 2, 1));
        handler.Requests.ShouldAllBe(r => r.Headers.GetValues("x-apisports-key").Single() == "test-key");
    }

    [Fact]
    public async Task A_spent_quota_keeps_the_last_good_poll_and_the_refresh_window_spares_the_budget()
    {
        var quotaSpent = false;
        var (adapter, handler) = Build(path => quotaSpent ? "quota-exceeded.json"
            : path.StartsWith("fixtures", StringComparison.Ordinal) ? "fixtures.json"
            : path.Contains("bet=1", StringComparison.Ordinal) ? "odds-match-winner.json" : "odds-goals-over-under.json");
        var good = await adapter.PollAsync(Now, CancellationToken.None);

        (await adapter.PollAsync(Now.AddMinutes(5), CancellationToken.None)).ShouldBeSameAs(good);
        handler.Requests.Count.ShouldBe(3);

        quotaSpent = true;
        (await adapter.PollAsync(Now.AddMinutes(31), CancellationToken.None)).ShouldBeSameAs(good);
    }

    private static (ApiFootballFeedAdapter, RecordedHandler) Build(Func<string, string> fileFor)
    {
        var handler = new RecordedHandler(fileFor);
        var factory = new SingleClientFactory(new HttpClient(handler) { BaseAddress = new Uri("https://v3.football.api-sports.io/") });
        var options = Options.Create(new ApiFootballOptions { Enabled = true, ApiKey = "test-key", LeagueIds = [39], Season = 2026, RefreshMinutes = 30 });
        return (new ApiFootballFeedAdapter(factory, options, new FakeTimeProvider(Now), NullLogger<ApiFootballFeedAdapter>.Instance), handler);
    }

    private sealed class RecordedHandler(Func<string, string> fileFor) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var file = Path.Combine(AppContext.BaseDirectory, "ApiFootball", "Responses", fileFor(request.RequestUri!.PathAndQuery.TrimStart('/')));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(await File.ReadAllTextAsync(file, cancellationToken), System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
