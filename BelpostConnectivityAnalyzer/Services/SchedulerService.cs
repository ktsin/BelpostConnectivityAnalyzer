using BelpostConnectivityAnalyzer.Configuration;
using Microsoft.Extensions.Options;

namespace BelpostConnectivityAnalyzer.Services;

public sealed class SchedulerService(IOptions<SyncSettings> options, ILogger<SchedulerService> logger)
{
    private readonly TimeOnly _syncTime = TimeOnly.Parse(options.Value.SyncTimeUtc);

    /// <summary>
    /// Waits until the next occurrence of the configured sync time.
    /// Returns false if cancellation was requested.
    /// </summary>
    public async Task<bool> WaitForNextSyncAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var todaySync = new DateTime(now.Year, now.Month, now.Day,
            _syncTime.Hour, _syncTime.Minute, 0, DateTimeKind.Utc);

        var nextSync = todaySync > now ? todaySync : todaySync.AddDays(1);
        var delay = nextSync - now;

        logger.LogInformation("Next sync scheduled at {NextSync:u} UTC (in {Delay:hh\\:mm\\:ss})",
            nextSync, delay);

        try
        {
            await Task.Delay(delay, ct);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}