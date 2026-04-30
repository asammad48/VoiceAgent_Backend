using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VoiceAgent.WorkerService.Workers;

public class OutboundDialerWorker(ILogger<OutboundDialerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboundDialerWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("OutboundDialerWorker heartbeat at: {TimeUtc}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
