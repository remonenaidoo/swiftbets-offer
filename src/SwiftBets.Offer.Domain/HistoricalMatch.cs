namespace SwiftBets.Offer.Domain;

public sealed record HistoricalMatch(int Index, string HomeTeam, string AwayTeam, int HomeGoals, int AwayGoals, HistoricalPrices Opening, HistoricalPrices Closing);
