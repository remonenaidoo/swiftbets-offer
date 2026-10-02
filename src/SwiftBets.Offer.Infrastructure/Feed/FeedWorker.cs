using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftBets.Offer.Application.Feed;
using SwiftBets.Offer.Application.Replay;

namespace SwiftBets.Offer.Infrastructure.Feed;

/// <summary>Polls the configured feed on a fixed tick and applies it, then checks staleness even if the poll failed.</summary>
public sealed partial class FeedWorker(FeedSync sync, StalenessGuard staleness, IOptions<ReplayOptions> options, TimeProvider time, ILogger<FeedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.TickSeconds), time);
        do
        {
            try
            {
                var changes = await sync.TickAsync(stoppingToken);
                if (changes > 0)
                {
                    LogTick(changes);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogTickFailed(ex);
            }

            try
            {
                await staleness.CheckAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogStalenessFailed(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Feed tick applied {Changes} changes")]
    private partial void LogTick(int changes);

    [LoggerMessage(Level = LogLevel.Error, Message = "Staleness check failed; retrying on the next tick")]
    private partial void LogStalenessFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Feed tick failed; retrying on the next tick")]
    private partial void LogTickFailed(Exception exception);
}
