using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VoiceAgent.WorkerService.Workers;

public class RecordingProcessingWorker(ILogger<RecordingProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RecordingProcessingWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("RecordingProcessingWorker heartbeat at: {TimeUtc}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
