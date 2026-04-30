namespace VoiceAgent.WorkerService.Workers;

public class PlaceholderWorker(ILogger<PlaceholderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Placeholder worker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker heartbeat at: {time}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
