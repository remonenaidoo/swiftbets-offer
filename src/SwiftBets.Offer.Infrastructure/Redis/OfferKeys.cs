namespace SwiftBets.Offer.Infrastructure.Redis;

/// <summary>
/// The Redis layout other services may read (placement checks price and version here). Each fixture hash holds
/// <c>version</c> and <c>snapshot</c> (a <c>FixtureChangedV1</c> JSON document).
/// </summary>
public static class OfferKeys
{
    public const string OpenFixtures = "offer:open";

    public static string Fixture(string fixtureId) => $"offer:fixture:{fixtureId}";

    public static string ResultPublished(string fixtureId, int resultVersion) => $"offer:result:{fixtureId}:v{resultVersion}";
}
