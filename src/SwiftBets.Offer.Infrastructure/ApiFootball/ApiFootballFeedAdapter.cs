using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftBets.Offer.Application.Feed;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Infrastructure.ApiFootball;

/// <summary>
/// API-Football v3 as a feed. Calls the provider at most once per refresh window (it has a daily request budget) and serves
/// the last good poll in between and through an outage. Staleness is judged from the provider's own odds timestamp.
/// </summary>
public sealed partial class ApiFootballFeedAdapter(IHttpClientFactory httpFactory, IOptions<ApiFootballOptions> options, TimeProvider time, ILogger<ApiFootballFeedAdapter> logger) : IFeedAdapter, IDisposable
{
    public const string ClientName = "api-football";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private FeedPoll _last = new([], []);
    private DateTimeOffset _nextRefresh = DateTimeOffset.MinValue;

    public string Name => "api-football";

    public async Task<FeedPoll> PollAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (now < _nextRefresh)
        {
            return _last;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (now < _nextRefresh)
            {
                return _last;
            }

            _nextRefresh = now.AddMinutes(options.Value.RefreshMinutes);
            var http = httpFactory.CreateClient(ClientName);
            var fixtures = new List<FixtureItem>();
            var odds = new List<OddsItem>();
            var from = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1));
            var to = DateOnly.FromDateTime(now.UtcDateTime.AddDays(options.Value.DaysAhead));
            foreach (var league in options.Value.LeagueIds)
            {
                var query = $"league={league}&season={options.Value.Season}";
                fixtures.AddRange(await GetAsync<FixtureItem>(http, $"fixtures?{query}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", cancellationToken));
                odds.AddRange(await GetAsync<OddsItem>(http, $"odds?{query}&bet={ApiFootballMapper.MatchWinnerBet}", cancellationToken));
                odds.AddRange(await GetAsync<OddsItem>(http, $"odds?{query}&bet={ApiFootballMapper.GoalsOverUnderBet}", cancellationToken));
            }

            _last = ApiFootballMapper.Map(fixtures, Merge(odds));
            return _last;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or ApiFootballException && !cancellationToken.IsCancellationRequested)
        {
            LogPollFailed(ex, time.GetUtcNow() - _nextRefresh + TimeSpan.FromMinutes(options.Value.RefreshMinutes));
            return _last;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<T>> GetAsync<T>(HttpClient http, string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-apisports-key", options.Value.ApiKey);
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<T>>(Json, cancellationToken) ?? throw new ApiFootballException("empty body");

        // The provider reports quota and key problems as a 200 with a non-empty errors object or array.
        var hasErrors = envelope.Errors.ValueKind switch
        {
            JsonValueKind.Object => envelope.Errors.EnumerateObject().Any(),
            JsonValueKind.Array => envelope.Errors.GetArrayLength() > 0,
            _ => false,
        };
        return hasErrors ? throw new ApiFootballException(envelope.Errors.GetRawText()) : envelope.Response;
    }

    /// <summary>Odds arrive one bet type per call; join each fixture's bookmakers so both markets map together.</summary>
    private static List<OddsItem> Merge(List<OddsItem> odds) =>
        [.. odds.GroupBy(o => o.Fixture.Id).Select(g => new OddsItem(g.First().Fixture, g.Max(o => o.Update),
            [.. g.SelectMany(o => o.Bookmakers).GroupBy(b => b.Id).Select(b => new Bookmaker(b.Key, b.First().Name, [.. b.SelectMany(x => x.Bets)]))]))];

    public void Dispose() => _gate.Dispose();

    [LoggerMessage(Level = LogLevel.Warning, Message = "API-Football poll failed; serving the last good poll (age {Age})")]
    private partial void LogPollFailed(Exception exception, TimeSpan age);
}

public sealed class ApiFootballException(string message) : Exception(message);
