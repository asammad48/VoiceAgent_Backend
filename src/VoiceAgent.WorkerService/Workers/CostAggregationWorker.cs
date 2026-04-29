namespace VoiceAgent.WorkerService.Workers;

public class CostAggregationWorker
{
    // Responsibilities:
    // 1) Aggregate call/provider usage per tenant/campaign/month
    // 2) Set WarningExceeded if cap crossed
    // 3) Do not auto-block unless SuperAdmin blocks manually
}
