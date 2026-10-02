namespace SwiftBets.Offer.Application.Catalog;

public sealed record CatalogFixture(string SportId, string CompetitionId, string CompetitionName, string FixtureId, string HomeTeam, string AwayTeam, DateTimeOffset KickoffAt, string Status, long OfferVersion);

public sealed record CatalogCompetition(string CompetitionId, string Name, int UpcomingFixtures);

public sealed record CatalogSport(string SportId, string Name, IReadOnlyList<CatalogCompetition> Competitions);

public static class CatalogIds
{
    /// <summary>A stable, URL-safe id from a display name: "Premier League" becomes "premier-league".</summary>
    public static string Slug(string name) =>
        string.Join('-', new string([.. name.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : ' ')]).Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
