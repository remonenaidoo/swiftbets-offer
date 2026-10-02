using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.Contracts.Trading;
using SwiftBets.Offer.Application.Ports;

namespace SwiftBets.Offer.Infrastructure.Messaging;

public sealed class ManualResultRejectedConsumer(IManualResultLog log) : IEventHandler<ManualResultRejectedV1>
{
    public Task HandleAsync(ConsumedEvent<ManualResultRejectedV1> message, CancellationToken cancellationToken) =>
        log.RecordRejectionAsync(message.Envelope.Payload, cancellationToken);
}
