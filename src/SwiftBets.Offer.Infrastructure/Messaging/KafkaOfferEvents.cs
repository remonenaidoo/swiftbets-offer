using SwiftBets.BuildingBlocks.Core;
using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.Contracts.Messaging;
using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Trading;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Infrastructure.Messaging;

/// <summary>
/// Publishes straight to Kafka: the offer store is Redis, which cannot share a transaction with an outbox. That is safe
/// here because snapshots are full and versioned (a re-send is harmless) and a duplicate result is a settlement no-op.
/// </summary>
public sealed class KafkaOfferEvents(IEventPublisher publisher, TimeProvider time) : IOfferEvents
{
    public Task FixtureChangedAsync(FixtureChangedV1 snapshot, CancellationToken cancellationToken) =>
        publisher.PublishAsync(Topics.FixtureChanged, snapshot.FixtureId, Envelope(snapshot), cancellationToken);

    public Task ResultPublishedAsync(ResultPublishedV1 result, CancellationToken cancellationToken) =>
        publisher.PublishAsync(Topics.ResultPublished, result.FixtureId, Envelope(result), cancellationToken);

    public Task ManualResultAsync(ManualResultV1 result, CancellationToken cancellationToken) =>
        publisher.PublishAsync(Topics.ManualResult, result.FixtureId, Envelope(result), cancellationToken);

    public Task MarketStatusChangedAsync(MarketStatusChangedV1 change, CancellationToken cancellationToken) =>
        publisher.PublishAsync(Topics.MarketStatusChanged, change.FixtureId, Envelope(change), cancellationToken);

    private EventEnvelope<T> Envelope<T>(T payload)
        where T : IEventContract =>
        EventEnvelope<T>.Create(payload, time.GetUtcNow(), CorrelationContext.CorrelationId ?? CorrelationContext.NewId());
}
