using SwiftBets.Contracts.Offer;

namespace SwiftBets.Offer.Application.Feed;

/// <summary>The feed's view of one fixture: its status and every market as the feed prices it.</summary>
public sealed record FeedFixture(string FixtureId, string Competition, string HomeTeam, string AwayTeam, DateTimeOffset KickoffAt, FixtureStatus Status, IReadOnlyList<MarketV1> Markets);

/// <summary>A result as the feed reports it; <c>Version</c> rises with each correction.</summary>
public sealed record FeedResult(string FixtureId, int Version, ResultStatus Status, int HomeGoals, int AwayGoals);

public sealed record FeedPoll(IReadOnlyList<FeedFixture> Fixtures, IReadOnlyList<FeedResult> Results);
