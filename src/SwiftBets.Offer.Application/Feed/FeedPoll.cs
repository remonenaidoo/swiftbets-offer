using SwiftBets.Contracts.Offer;

namespace SwiftBets.Offer.Application.Feed;

/// <summary>
/// The feed's view of one fixture: its status and every market as the feed prices it. <c>UpdatedAt</c> is when the
/// provider last refreshed it; staleness is judged from it, not from when we happened to poll.
/// </summary>
public sealed record FeedFixture(string FixtureId, string Competition, string HomeTeam, string AwayTeam, DateTimeOffset KickoffAt, FixtureStatus Status, IReadOnlyList<MarketV1> Markets, DateTimeOffset UpdatedAt);

/// <summary>A result as the feed reports it; <c>Version</c> rises with each correction.</summary>
public sealed record FeedResult(string FixtureId, int Version, ResultStatus Status, int HomeGoals, int AwayGoals);

public sealed record FeedPoll(IReadOnlyList<FeedFixture> Fixtures, IReadOnlyList<FeedResult> Results);
