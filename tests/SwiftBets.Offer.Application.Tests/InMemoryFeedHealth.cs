using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Tests;

internal sealed class InMemoryFeedHealth : IFeedHealthStore
{
    private readonly Dictionary<string, DateTimeOffset> _seen = [];
    private readonly Dictionary<string, IReadOnlyList<string>> _stale = [];

    public Task RecordSeenAsync(string fixtureId, DateTimeOffset updatedAt, CancellationToken cancellationToken)
    {
        _seen[fixtureId] = updatedAt;
        return Task.CompletedTask;
    }

    public Task<DateTimeOffset?> GetSeenAsync(string fixtureId, CancellationToken cancellationToken) =>
        Task.FromResult(_seen.TryGetValue(fixtureId, out var seen) ? seen : (DateTimeOffset?)null);

    public Task<IReadOnlyList<string>?> GetStaleSuspendedAsync(string fixtureId, CancellationToken cancellationToken) =>
        Task.FromResult(_stale.GetValueOrDefault(fixtureId));

    public Task SetStaleSuspendedAsync(string fixtureId, IReadOnlyList<string> marketIds, CancellationToken cancellationToken)
    {
        _stale[fixtureId] = marketIds;
        return Task.CompletedTask;
    }

    public Task ClearStaleSuspendedAsync(string fixtureId, CancellationToken cancellationToken)
    {
        _stale.Remove(fixtureId);
        return Task.CompletedTask;
    }
}
