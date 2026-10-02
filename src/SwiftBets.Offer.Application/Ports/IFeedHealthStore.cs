namespace SwiftBets.Offer.Application.Ports;

/// <summary>How fresh each fixture's feed data is, and which markets staleness (not a trader) suspended.</summary>
public interface IFeedHealthStore
{
    Task RecordSeenAsync(string fixtureId, DateTimeOffset updatedAt, CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetSeenAsync(string fixtureId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>?> GetStaleSuspendedAsync(string fixtureId, CancellationToken cancellationToken);

    Task SetStaleSuspendedAsync(string fixtureId, IReadOnlyList<string> marketIds, CancellationToken cancellationToken);

    Task ClearStaleSuspendedAsync(string fixtureId, CancellationToken cancellationToken);
}
