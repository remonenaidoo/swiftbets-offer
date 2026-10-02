using SwiftBets.Offer.Application.Feed;

namespace SwiftBets.Offer.Application.Ports;

/// <summary>
/// A source of fixtures, prices and results: the recorded-season replay in dev and demo, a real provider elsewhere.
/// Each poll returns the feed's current view; the sync applies it idempotently, so a poll can repeat or overlap freely.
/// </summary>
public interface IFeedAdapter
{
    string Name { get; }

    Task<FeedPoll> PollAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
