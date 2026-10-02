using System.ComponentModel.DataAnnotations;

namespace SwiftBets.Offer.Application.Feed;

public sealed class FeedOptions
{
    public const string SectionName = "Feed";

    /// <summary>A scheduled fixture whose feed data is older than this has its open markets suspended until it refreshes.</summary>
    [Range(10, 3600)]
    public int StaleAfterSeconds { get; set; } = 120;
}
