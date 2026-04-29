namespace VoiceAgent.Application.Services.Restaurant;

public interface IRestaurantMenuService;
public interface IRestaurantDealService;

public interface IRestaurantDiscoveryService
{
    Task<RestaurantDiscoveryResponse> DiscoverAsync(RestaurantDiscoveryRequest request, CancellationToken cancellationToken = default);
}

public interface IRestaurantCartService;

public interface IRestaurantPricingService
{
    Task<RestaurantOrderPricing> CalculateOfficialTotalAsync(Guid callSessionId, CancellationToken cancellationToken = default);
}

public interface IRestaurantOrderService
{
    Task<string> BuildCapturedOnlyOrderJsonAsync(Guid callSessionId, CancellationToken cancellationToken = default);
}

public interface IDeliveryCoverageService
{
    Task<DeliveryCoverageResult> CheckCoverageAsync(DeliveryCoverageRequest request, CancellationToken cancellationToken = default);
}
