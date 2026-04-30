using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VoiceAgent.WorkerService.Workers;

public class CallSummaryWorker(ILogger<CallSummaryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CallSummaryWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("CallSummaryWorker heartbeat at: {TimeUtc}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
