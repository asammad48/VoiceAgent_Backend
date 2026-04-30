using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VoiceAgent.WorkerService.Workers;

public class WebhookRetryWorker(ILogger<WebhookRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WebhookRetryWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("WebhookRetryWorker heartbeat at: {TimeUtc}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
