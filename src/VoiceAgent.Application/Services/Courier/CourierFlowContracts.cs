namespace VoiceAgent.Application.Services.Courier;

public enum CourierIntent
{
    AskCourierPrice,
    CreateCourierQuote,
    CreateCourierOrder,
    ProvidePickupAddress,
    ProvideDropoffAddress,
    ProvideWeight,
    ProvidePackageType,
    ProvideUrgency,
    AskDeliveryTime,
    ConfirmCourierOrder,
    CancelCourierOrder
}

public static class CourierRequiredSlots
{
    public static readonly IReadOnlyCollection<string> Slots =
    [
        "PickupAddress",
        "DropoffAddress",
        "PackageWeight",
        "PackageType",
        "Urgency",
        "CustomerPhone"
    ];
}

public sealed record CourierFlowRequest(
    Guid TenantId,
    Guid ClientId,
    Guid CampaignId,
    Guid? BranchId,
    Guid CallSessionId,
    string UserMessage,
    CourierIntent Intent);

public sealed record CourierAddressValidationResult(
    bool Success,
    string? NormalizedAddressJson,
    string? FailureMessage);

public sealed record CourierRouteEstimate(
    decimal DistanceKm,
    int DurationMinutes);

public sealed record CourierPricingBreakdown(
    decimal BaseFee,
    decimal DistanceFee,
    decimal WeightFee,
    decimal UrgencyFee,
    decimal ZoneFee,
    decimal Total,
    string Currency,
    DateTime? EstimatedDeliveryTime);

public static class CourierFlowGuardrails
{
    public const string OfficialPricingRule =
        "Courier totals and delivery times must come from pricing/distance tools only; the LLM must never invent either value.";

    public const string GeocodingFailureRule =
        "If geocoding fails, ask for address/postcode again and do not continue to quote.";

    public static readonly IReadOnlyCollection<string> FlowSequence =
    [
        "Greeting",
        "Identify courier intent",
        "Collect pickup/dropoff",
        "Geocode addresses",
        "Calculate route distance/duration using OSRM",
        "Collect weight/package/urgency",
        "Apply tenant/client pricing profile",
        "Estimate price and time",
        "Confirm",
        "Save internally or dispatch externally"
    ];
}
