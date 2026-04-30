namespace VoiceAgent.Application.Services.Sales;

public static class OutboundCampaignCatalog
{
    public const string MedicareSales = "MedicareSales";
    public const string AcaSales = "AcaSales";
    public const string FeSales = "FeSales";
}

public interface IOutboundDialerOrchestrator
{
    Task<OutboundDialCycleResult> RunCycleAsync(CancellationToken cancellationToken);
}

public sealed record OutboundDialCycleResult(
    bool CampaignFound,
    bool AttemptCreated,
    bool CallInitiated,
    string Outcome,
    Guid? CampaignId,
    Guid? LeadId,
    Guid? AttemptId);

public sealed record OutboundCampaignSnapshot(
    Guid TenantId,
    Guid ClientId,
    Guid CampaignId,
    string CampaignName,
    int CallCap,
    int MinuteCap,
    bool WarningBlocked,
    bool HumanTransferEnabled,
    string QualificationScript,
    string ObjectionScript,
    int MaxAttemptCount,
    int RetryDelayMinutes);

public sealed record EligibleOutboundLead(
    Guid LeadId,
    string FullName,
    string Phone,
    int AttemptCount,
    DateTime? LastAttemptAt,
    bool DoNotCall,
    bool OptedOut);

public sealed record OutboundAttemptCreateRequest(
    Guid TenantId,
    Guid ClientId,
    Guid CampaignId,
    Guid LeadId,
    int AttemptNumber);

public sealed record OutboundAttemptCreated(
    Guid AttemptId,
    int AttemptNumber);

public sealed record OutboundCallInitiationRequest(
    Guid TenantId,
    Guid ClientId,
    Guid CampaignId,
    Guid AttemptId,
    string ToPhone,
    bool HumanTransferEnabled,
    string QualificationScript,
    string ObjectionScript);

public interface IOutboundCampaignRuntimeReader
{
    Task<OutboundCampaignSnapshot?> GetNextActiveCampaignAsync(CancellationToken cancellationToken);
    Task<EligibleOutboundLead?> GetNextEligibleLeadAsync(OutboundCampaignSnapshot campaign, CancellationToken cancellationToken);
}

public interface IOutboundAttemptWriter
{
    Task<OutboundAttemptCreated> CreateAttemptAsync(OutboundAttemptCreateRequest request, CancellationToken cancellationToken);
    Task RecordCallOutcomeAsync(Guid attemptId, string outcome, string? disposition, CancellationToken cancellationToken);
}

public interface IRetryEligibilityPolicy
{
    bool CanRetry(EligibleOutboundLead lead, OutboundCampaignSnapshot campaign, DateTime utcNow);
}
