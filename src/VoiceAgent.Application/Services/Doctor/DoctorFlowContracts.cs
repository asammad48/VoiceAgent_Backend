namespace VoiceAgent.Application.Services.Doctor;

public static class DoctorRequiredSlots
{
    public static readonly IReadOnlyCollection<string> Slots =
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

public sealed record DoctorFlowRequest(
    Guid TenantId,
    Guid ClientId,
    Guid CampaignId,
    Guid? BranchId,
    Guid CallSessionId,
    string UserMessage);

public sealed record EmergencyScreeningResult(
    bool EmergencyDetected,
    bool ShouldHandoff,
    string SafetyResponse);

public sealed record DoctorAvailabilityResult(
    bool IsAvailable,
    DateTime? AppointmentDateTime,
    string? DoctorName,
    string? Message);

public static class DoctorFlowGuardrails
{
    public const string NoDiagnosisRule =
        "The agent must not provide medical diagnosis.";

    public const string HighRiskRule =
        "If serious symptoms are described, trigger high-risk handling and either transfer (if enabled) or provide emergency/local clinic safety instruction.";

    public static readonly IReadOnlyCollection<string> FlowSequence =
    [
        "Greeting",
        "Detect appointment intent",
        "Ask emergency screening question if configured",
        "If emergency detected, advise urgent/emergency contact and optionally handoff",
        "Collect patient details",
        "Check availability",
        "Confirm appointment",
        "Save internally or dispatch externally"
    ];
}
