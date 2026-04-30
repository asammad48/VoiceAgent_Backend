namespace VoiceAgent.WorkerService.Workers;

public class CostAggregationWorker
{
    // Responsibilities:
    // 1) Aggregate provider usage per tenant/client/campaign/month
    // 2) Track total calls, minutes, provider cost, monthly caps, call caps
    // 3) Set WarningExceeded if cap crossed
    // 4) Do not auto-block unless SuperAdmin blocks manually
}
