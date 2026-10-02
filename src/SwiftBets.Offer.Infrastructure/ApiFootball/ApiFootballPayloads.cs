using System.Text.Json.Serialization;

namespace SwiftBets.Offer.Infrastructure.ApiFootball;

// The provider's documented v3 envelope and the fields we read; everything else is ignored.
internal sealed record Envelope<T>([property: JsonPropertyName("errors")] System.Text.Json.JsonElement Errors, [property: JsonPropertyName("response")] IReadOnlyList<T> Response);

internal sealed record FixtureItem(FixtureInfo Fixture, LeagueInfo League, TeamPair Teams, GoalPair Goals, ScoreInfo? Score);

internal sealed record FixtureInfo(long Id, DateTimeOffset Date, StatusInfo Status);

internal sealed record StatusInfo(string Short);

internal sealed record LeagueInfo(int Id, string Name, int Season);

internal sealed record TeamPair(TeamInfo Home, TeamInfo Away);

internal sealed record TeamInfo(long Id, string Name);

internal sealed record GoalPair(int? Home, int? Away);

internal sealed record ScoreInfo(GoalPair? Fulltime);

internal sealed record OddsItem(OddsFixture Fixture, DateTimeOffset Update, IReadOnlyList<Bookmaker> Bookmakers);

internal sealed record OddsFixture(long Id);

internal sealed record Bookmaker(int Id, string Name, IReadOnlyList<Bet> Bets);

internal sealed record Bet(int Id, string Name, IReadOnlyList<BetValue> Values);

internal sealed record BetValue(string Value, string Odd);
