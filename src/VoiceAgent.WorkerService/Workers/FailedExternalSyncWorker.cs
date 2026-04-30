using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VoiceAgent.WorkerService.Workers;

public class FailedExternalSyncWorker(ILogger<FailedExternalSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FailedExternalSyncWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("FailedExternalSyncWorker heartbeat at: {TimeUtc}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
