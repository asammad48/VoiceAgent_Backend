namespace VoiceAgent.Application.Services.Courier;

public interface ICourierPricingService
{
    Task<CourierPricingBreakdown> EstimateAsync(CourierFlowRequest request, CancellationToken cancellationToken = default);
}

public interface ICourierQuoteService
{
    Task<CourierRouteEstimate> BuildQuoteAsync(CourierFlowRequest request, CancellationToken cancellationToken = default);
}

public interface ICourierOrderService
{
    Task<string> BuildCapturedOnlyOrderJsonAsync(Guid callSessionId, CancellationToken cancellationToken = default);
}

public interface IDistanceCalculationService
{
    Task<CourierAddressValidationResult> GeocodeAsync(string rawAddress, CancellationToken cancellationToken = default);
    Task<CourierRouteEstimate> RouteAsync(string pickupAddressJson, string dropoffAddressJson, CancellationToken cancellationToken = default);
}
