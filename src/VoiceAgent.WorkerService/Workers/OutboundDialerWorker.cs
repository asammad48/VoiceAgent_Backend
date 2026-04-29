namespace VoiceAgent.WorkerService.Workers;

public class OutboundDialerWorker
{
    // Responsibilities:
    // 1) Load active outbound campaigns
    // 2) Check tenant/campaign call caps and warning state
    // 3) Load eligible leads
    // 4) Create OutboundAttempt
    // 5) Initiate call through ITelephonyProvider
    // 6) Update attempt status
}
