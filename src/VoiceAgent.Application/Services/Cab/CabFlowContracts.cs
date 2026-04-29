namespace VoiceAgent.Application.Services.Cab;

public static class CabRequiredSlots
{
    public static readonly IReadOnlyCollection<string> Slots =
    [
        "PickupLocation",
        "DropoffLocation",
        "DateTime",
        "PassengerCount",
        "VehicleType",
        "CustomerPhone",
        "SpecialNotes"
    ];
}

public sealed record CabFlowRequest(
    Guid TenantId,
    Guid ClientId,
    Guid CampaignId,
    Guid? BranchId,
    Guid CallSessionId,
    string UserMessage);

public sealed record CabRouteEstimate(
    decimal DistanceKm,
    int DurationMinutes);

public sealed record CabFareEstimate(
    decimal EstimatedFare,
    string Currency,
    string SourceTool);

public static class CabFlowGuardrails
{
    public const string FareRule =
        "Fare estimate must come from a pricing service/tool and never be invented by the LLM.";

    public const string ExternalConfirmationRule =
        "When an external booking API is configured, booking is confirmed only after external API success.";

    public const string CapturedOnlyRule =
        "When no external booking API is configured, save booking status as CapturedOnly.";

    public static readonly IReadOnlyCollection<string> FlowSequence =
    [
        "Greeting",
        "Detect cab booking intent",
        "Collect pickup/dropoff",
        "Geocode locations",
        "Calculate distance/time using OSRM",
        "Estimate fare using client campaign config",
        "Collect passenger count and vehicle type",
        "Confirm booking",
        "Save internally or dispatch externally"
    ];
}
