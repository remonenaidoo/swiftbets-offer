using System.Text.Json;
using SwiftBets.Offer.Application.Ports;
using SwiftBets.Offer.Domain;

namespace SwiftBets.Offer.Infrastructure.Season;

/// <summary>The 2024/25 Premier League season (football-data.co.uk, Bet365 opening and closing prices), shipped in the assembly.</summary>
public sealed class EmbeddedSeasonSource : ISeasonSource
{
    public EmbeddedSeasonSource()
    {
        using var stream = typeof(EmbeddedSeasonSource).Assembly.GetManifestResourceStream("SwiftBets.Offer.Infrastructure.Data.epl-2024-25.json")!;
        var file = JsonSerializer.Deserialize<SeasonFile>(stream, JsonSerializerOptions.Web)!;
        Competition = file.Competition;
        Season = file.Season;
        Matches = [.. file.Matches.Select((m, i) => new HistoricalMatch(
            i, m.Home, m.Away, m.HomeGoals, m.AwayGoals,
            new HistoricalPrices(m.Open.Home, m.Open.Draw, m.Open.Away, m.Open.Over25, m.Open.Under25),
            new HistoricalPrices(m.Close.Home, m.Close.Draw, m.Close.Away, m.Close.Over25, m.Close.Under25)))];
    }

    public string Competition { get; }

    public string Season { get; }

    public IReadOnlyList<HistoricalMatch> Matches { get; }

    private sealed record SeasonFile(string Competition, string Season, IReadOnlyList<MatchRow> Matches);

    private sealed record MatchRow(string Home, string Away, int HomeGoals, int AwayGoals, PriceRow Open, PriceRow Close);

    private sealed record PriceRow(decimal Home, decimal Draw, decimal Away, decimal Over25, decimal Under25);
}
