namespace VoiceAgent.Application.Services.Core;

public interface IAgentTool
{
    string Name { get; }
    IReadOnlyCollection<string> RequiredSlots { get; }

    Task<ToolExecutionResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken);
}

public sealed class ToolExecutionContext
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string CorrelationId { get; set; } = default!;
    public string? UserMessage { get; set; }
    public Dictionary<string, object?> Slots { get; set; } = new();
    public Dictionary<string, object?> Memory { get; set; } = new();
}

public sealed class ToolExecutionResult
{
    public bool Success { get; set; }
    public string ToolName { get; set; } = default!;
    public string? UserMessage { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ShouldRetry { get; set; }
    public bool RequiresHumanHandoff { get; set; }
}


public static class RestaurantIntentCatalog
{
    public static readonly IReadOnlyCollection<string> SupportedIntents =
    [
        "AskMenuCategories",
        "AskDeals",
        "AskDishInfo",
        "SearchMenuItem",
        "AddItemToCart",
        "RemoveItemFromCart",
        "ChangeQuantity",
        "AskCurrentTotal",
        "SetDeliveryOrPickup",
        "ProvideAddress",
        "ProvidePhone",
        "ConfirmOrder",
        "CancelOrder",
        "AskBusinessHours",
        "AskDeliveryArea"
    ];
}


public static class CourierIntentCatalog
{
    public static readonly IReadOnlyCollection<string> SupportedIntents =
    [
        "AskCourierPrice",
        "CreateCourierQuote",
        "CreateCourierOrder",
        "ProvidePickupAddress",
        "ProvideDropoffAddress",
        "ProvideWeight",
        "ProvidePackageType",
        "ProvideUrgency",
        "AskDeliveryTime",
        "ConfirmCourierOrder",
        "CancelCourierOrder"
    ];

    public static readonly IReadOnlyCollection<string> RequiredSlots =
    [
        "PickupAddress",
        "DropoffAddress",
        "PackageWeight",
        "PackageType",
        "Urgency",
        "CustomerPhone"
    ];
}


public static class CabIntentCatalog
{
    public static readonly IReadOnlyCollection<string> SupportedIntents =
    [
        "CreateCabBooking",
        "ProvidePickupLocation",
        "ProvideDropoffLocation",
        "ProvideDateTime",
        "ProvidePassengerCount",
        "ProvideVehicleType",
        "ProvideCustomerPhone",
        "ProvideSpecialNotes",
        "AskFareEstimate",
        "ConfirmCabBooking",
        "CancelCabBooking"
    ];

    public static readonly IReadOnlyCollection<string> RequiredSlots =
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


public static class DoctorIntentCatalog
{
    public static readonly IReadOnlyCollection<string> SupportedIntents =
    [
        "CreateAppointment",
        "ProvidePatientName",
        "ProvidePhone",
        "ProvidePatientType",
        "ProvideReasonForVisit",
        "ProvidePreferredDoctor",
        "ProvidePreferredDateTime",
        "ProvideClinicBranch",
        "ConfirmAppointment",
        "CancelAppointment",
        "EmergencyHelp"
    ];

    public static readonly IReadOnlyCollection<string> RequiredSlots =
    [
        "PatientName",
        "Phone",
        "PatientType",
        "ReasonForVisit",
        "PreferredDoctor",
        "PreferredDateTime",
        "ClinicBranch"
    ];
}

public static class CampaignToolCatalog
{
    public static readonly IReadOnlyCollection<string> RestaurantTools =
    ["ListCategoriesTool","ListDealsTool","DealSearchTool","MenuItemSearchTool","DishInfoTool","CartUpdateTool","CalculateRestaurantTotalTool","DeliveryCoverageTool","SaveRestaurantOrderTool","ExternalRestaurantDispatchTool"];

    public static readonly IReadOnlyCollection<string> CourierTools =
    ["GeocodeAddressTool","RouteDistanceTool","CourierQuoteTool","SaveCourierOrderTool","ExternalCourierDispatchTool"];

    public static readonly IReadOnlyCollection<string> CabTools =
    ["CabRouteTool","CabFareEstimateTool","CabBookingTool","ExternalCabDispatchTool"];

    public static readonly IReadOnlyCollection<string> DoctorTools =
    ["DoctorAvailabilityTool","EmergencyDetectionTool","DoctorAppointmentBookingTool","ExternalDoctorDispatchTool"];

    public static readonly IReadOnlyCollection<string> SalesTools =
    ["LeadQualificationTool","SalesScriptTool","ObjectionHandlingTool","DispositionTool","HumanTransferTool"];

    public static readonly IReadOnlyCollection<string> GenericTools =
    ["RagAnswerTool","HumanHandoffTool","ExternalApiDispatchTool","CallRecordingTool"];
}
