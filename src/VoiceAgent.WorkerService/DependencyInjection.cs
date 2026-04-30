using Microsoft.Extensions.DependencyInjection;
using VoiceAgent.WorkerService.Workers;

namespace VoiceAgent.WorkerService;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        services.AddHostedService<OutboundDialerWorker>();
        services.AddHostedService<WebhookRetryWorker>();
        services.AddHostedService<CallSummaryWorker>();
        services.AddHostedService<RecordingProcessingWorker>();
        services.AddHostedService<KnowledgeIndexingWorker>();
        services.AddHostedService<CostAggregationWorker>();
        services.AddHostedService<CleanupWorker>();
        services.AddHostedService<FailedExternalSyncWorker>();
        return services;
    }
}
