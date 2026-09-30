using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftBets.Offer.Application.Replay;

namespace SwiftBets.Offer.Infrastructure.Replay;

public sealed partial class ReplayWorker(ReplayEngine engine, IOptions<ReplayOptions> options, TimeProvider time, ILogger<ReplayWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.TickSeconds), time);
        do
        {
            try
            {
                var changes = await engine.TickAsync(stoppingToken);
                if (changes > 0)
                {
                    LogTick(changes);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogTickFailed(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Replay tick applied {Changes} changes")]
    private partial void LogTick(int changes);

    [LoggerMessage(Level = LogLevel.Error, Message = "Replay tick failed; retrying on the next tick")]
    private partial void LogTickFailed(Exception exception);
}
