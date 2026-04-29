namespace VoiceAgent.Application.Services.Cab;

public interface ICabFareService
{
    Task<CabFareEstimate> EstimateFareAsync(CabFlowRequest request, CancellationToken cancellationToken = default);
}

public interface ICabBookingService
{
    Task<CabRouteEstimate> EstimateRouteAsync(string pickupAddress, string dropoffAddress, CancellationToken cancellationToken = default);
    Task<string> BuildBookingResultJsonAsync(Guid callSessionId, CancellationToken cancellationToken = default);
}
