using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Application.Orchestration;

public static class SecurityAndBillingPolicy
{
    public static readonly IReadOnlyCollection<UserPlatformRole> GlobalViewRoles = [UserPlatformRole.SuperAdmin];

    public const string TenantIsolationRule =
        "All non-SuperAdmin queries must filter by TenantId and ClientId; campaign-level data must also filter by CampaignId where available.";

    public const string WarningBehaviorRule =
        "When usage limit is crossed set WarningExceeded=true; do not auto-block tenant. Only SuperAdmin can block/unblock manually.";

    public static readonly IReadOnlyCollection<string> BillingMetrics =
    ["TenantUsage", "CampaignUsage", "TotalCalls", "TotalMinutes", "ProviderCost", "MonthlyCaps", "CallCaps"];

    public const string ComplianceExpansionRule =
        "Current flow uses basic filtering only; keep extension points for consent, DNC/opt-out, TCPA/GDPR/UK GDPR, and retention policies.";
}

public sealed record RecordingPolicy(
    bool Enabled,
    bool RecordInbound,
    bool RecordOutbound,
    string StorageProvider);
