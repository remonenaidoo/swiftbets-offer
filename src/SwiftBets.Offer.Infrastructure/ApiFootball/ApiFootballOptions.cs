using System.ComponentModel.DataAnnotations;

namespace SwiftBets.Offer.Infrastructure.ApiFootball;

/// <summary>API-Football v3. Off by default; replay stays the dev and demo feed until a key is configured.</summary>
public sealed class ApiFootballOptions
{
    public const string SectionName = "ApiFootball";

    public bool Enabled { get; set; }

    [Required]
    public string BaseAddress { get; set; } = "https://v3.football.api-sports.io";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Provider league ids to offer; 39 is the Premier League.</summary>
    [MinLength(1)]
    public int[] LeagueIds { get; set; } = [39];

    [Range(2000, 2100)]
    public int Season { get; set; } = 2026;

    /// <summary>Days ahead to list. The provider rejects open-ended ranges.</summary>
    [Range(1, 14)]
    public int DaysAhead { get; set; } = 7;

    /// <summary>
    /// Minimum minutes between provider calls. Each refresh costs two requests per league; the free tier allows 100 a day,
    /// so 30 minutes keeps one league inside it. Between refreshes the last good poll is served.
    /// </summary>
    [Range(1, 1440)]
    public int RefreshMinutes { get; set; } = 30;
}
