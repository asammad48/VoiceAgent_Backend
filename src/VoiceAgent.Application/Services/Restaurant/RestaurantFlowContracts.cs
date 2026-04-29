namespace VoiceAgent.Application.Services.Restaurant;

public enum RestaurantIntent
{
    AskMenuCategories,
    AskDeals,
    AskDishInfo,
    SearchMenuItem,
    AddItemToCart,
    RemoveItemFromCart,
    ChangeQuantity,
    AskCurrentTotal,
    SetDeliveryOrPickup,
    ProvideAddress,
    ProvidePhone,
    ConfirmOrder,
    CancelOrder,
    AskBusinessHours,
    AskDeliveryArea
}

public sealed record RestaurantDiscoveryRequest(
    Guid TenantId,
    Guid ClientId,
    Guid CampaignId,
    Guid? BranchId,
    string UserMessage,
    RestaurantIntent Intent);

public sealed record RestaurantDiscoveryResponse(
    string Summary,
    IReadOnlyCollection<string> SuggestedNextActions,
    IReadOnlyCollection<string> ToolChain);

public sealed record DeliveryCoverageRequest(
    Guid TenantId,
    Guid ClientId,
    Guid CampaignId,
    Guid BranchId,
    string RawAddress);

public sealed record DeliveryCoverageResult(
    bool IsDeliverable,
    decimal DistanceKm,
    decimal DeliveryFee,
    string? RejectionReason,
    string? SuggestedFallback);

public sealed record RestaurantOrderPricing(
    decimal Subtotal,
    decimal DeliveryFee,
    decimal Tax,
    decimal Discount,
    decimal Total,
    string Currency);

public static class RestaurantFlowGuardrails
{
    public const string DiscoveryDealsBehavior =
        "For deal questions use ListDealsTool/DealSearchTool, summarize briefly, then ask whether user wants details or to add one.";

    public const string DiscoveryMenuBehavior =
        "For menu questions use ListCategoriesTool and return categories first (not full menu).";

    public const string DiscoveryDishBehavior =
        "For dish questions use MenuItemSearchTool and return short matching descriptions with prices.";

    public const string OfficialPricingRule =
        "Only CalculateRestaurantTotalTool can produce official totals; the LLM must never calculate official totals.";

    public const string DeliveryCoverageRule =
        "Use Nominatim geocoding + OSRM route distance, compare with DeliveryRadiusKm, then compute delivery fee using distance bands.";
}
