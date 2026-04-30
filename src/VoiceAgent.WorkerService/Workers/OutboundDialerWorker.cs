using VoiceAgent.Application.Services.Sales;

namespace VoiceAgent.WorkerService.Workers;

public class OutboundDialerWorker(
    IOutboundDialerOrchestrator dialerOrchestrator)
{
    public async Task<OutboundDialCycleResult> ExecuteDialCycleAsync(CancellationToken cancellationToken)
    {
        // Outbound calls are intentionally handled in WorkerService
        // and not in a live WebAPI request path.
        return await dialerOrchestrator.RunCycleAsync(cancellationToken);
    }
}
