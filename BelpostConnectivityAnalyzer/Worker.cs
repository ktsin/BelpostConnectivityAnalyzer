using BelpostConnectivityAnalyzer.Services;

namespace BelpostConnectivityAnalyzer;

public sealed class Worker(
    SyncService syncService,
    SchedulerService schedulerService,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Belpost Connectivity Analyzer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            #if !DEBUG
            var shouldRun = await schedulerService.WaitForNextSyncAsync(stoppingToken);
            if (!shouldRun)
                break;
            #endif
            
            #if DEBUG
            logger.LogInformation("DEBUG mode: running sync immediately without waiting");
            await Task.Delay(1000, stoppingToken);
            #endif
            
            await syncService.RunAsync(stoppingToken);
        }

        logger.LogInformation("Belpost Connectivity Analyzer stopped");
    }
}