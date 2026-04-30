using VoiceAgent.Application.Services.Core;

namespace VoiceAgent.WorkerService.Workers;

public class FailedExternalSyncWorker(
    IExternalDispatchOrchestrator externalDispatchOrchestrator)
{
    public IExternalDispatchOrchestrator ExternalDispatchOrchestrator { get; } = externalDispatchOrchestrator;

    // Responsibilities:
    // 1) Find records with status CapturedPendingSync
    // 2) Load external API configuration
    // 3) Retry dispatch through IExternalDispatchOrchestrator
    // 4) Save request/response
    // 5) Update status
}
