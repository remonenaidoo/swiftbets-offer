using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Trading;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Application.Tests;

internal sealed class InMemoryOffer : IOfferStore, IOfferEvents, IManualResultLog
{
    private readonly Dictionary<string, FixtureChangedV1> _fixtures = [];
    private readonly HashSet<string> _results = [];

    public List<FixtureChangedV1> Published { get; } = [];

    public List<ResultPublishedV1> Results { get; } = [];

    public List<ManualResultV1> ManualResults { get; } = [];

    public List<MarketStatusChangedV1> StatusChanges { get; } = [];

    public Task<FixtureChangedV1?> GetAsync(string fixtureId, CancellationToken cancellationToken) =>
        Task.FromResult(_fixtures.GetValueOrDefault(fixtureId));

    public Task<IReadOnlyList<FixtureChangedV1>> ListOpenAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FixtureChangedV1>>([.. _fixtures.Values]);

    public Task<bool> TrySaveAsync(FixtureChangedV1 snapshot, long expectedVersion, CancellationToken cancellationToken)
    {
        if ((_fixtures.GetValueOrDefault(snapshot.FixtureId)?.OfferVersion ?? 0) != expectedVersion)
        {
            return Task.FromResult(false);
        }

        _fixtures[snapshot.FixtureId] = snapshot;
        return Task.FromResult(true);
    }

    public Task<bool> IsResultPublishedAsync(string fixtureId, int resultVersion, CancellationToken cancellationToken) => Task.FromResult(_results.Contains($"{fixtureId}:{resultVersion}"));

    public Task MarkResultPublishedAsync(string fixtureId, int resultVersion, CancellationToken cancellationToken)
    {
        _results.Add($"{fixtureId}:{resultVersion}");
        return Task.CompletedTask;
    }

    public Task FixtureChangedAsync(FixtureChangedV1 snapshot, CancellationToken cancellationToken)
    {
        Published.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task ResultPublishedAsync(ResultPublishedV1 result, CancellationToken cancellationToken)
    {
        Results.Add(result);
        return Task.CompletedTask;
    }

    public Task ManualResultAsync(ManualResultV1 result, CancellationToken cancellationToken)
    {
        ManualResults.Add(result);
        return Task.CompletedTask;
    }

    public Task MarketStatusChangedAsync(MarketStatusChangedV1 change, CancellationToken cancellationToken)
    {
        StatusChanges.Add(change);
        return Task.CompletedTask;
    }

    public Task RecordIssuedAsync(ManualResultV1 result, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RecordRejectionAsync(ManualResultRejectedV1 rejection, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<ManualResultOutcome?> GetAsync(Guid manualResultId, CancellationToken cancellationToken) => Task.FromResult<ManualResultOutcome?>(null);
}
