namespace VoiceAgent.WorkerService.Workers;

public class FailedExternalSyncWorker
{
    // Responsibilities:
    // 1) Find records with status CapturedPendingSync
    // 2) Load external API configuration
    // 3) Retry dispatch
    // 4) Save request/response
    // 5) Update status
}
