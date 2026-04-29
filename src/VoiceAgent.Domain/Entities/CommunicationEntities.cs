using VoiceAgent.Common.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Domain.Entities;

public class PhoneNumber : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? CampaignId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public CampaignDirection Direction { get; set; }
}

public class PromptVersion : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string? PromptRulesJson { get; set; }
    public int Version { get; set; }
}

public class CallSession : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid? BranchId { get; set; }
    public CallChannel Channel { get; set; }
    public CampaignDirection Direction { get; set; }
    public string? ExternalCallId { get; set; }
    public string? CallerPhone { get; set; }
    public string? CustomerName { get; set; }
    public CallSessionStatus Status { get; set; }
    public string? CurrentState { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
    public string? CollectedSlotsJson { get; set; }
    public string? FinalResultJson { get; set; }
    public string? SummaryJson { get; set; }
    public string? FailureReason { get; set; }
    public string? CorrelationId { get; set; }
    public bool HandoffAllowed { get; set; }
    public bool HandoffRequested { get; set; }
    public bool HandoffCompleted { get; set; }
    public string? HandoffReason { get; set; }
    public bool RecordingEnabled { get; set; }
    public string? RecordingUrl { get; set; }
}

public class CallTurn : AuditableEntity
{
    public Guid CallSessionId { get; set; }
    public int TurnNumber { get; set; }
    public string Speaker { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public decimal? Confidence { get; set; }
    public string? StateBefore { get; set; }
    public string? StateAfter { get; set; }
    public string? ToolName { get; set; }
    public string? ToolResultJson { get; set; }
    public int? LatencyMs { get; set; }
}

public class CallEvent : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EventDataJson { get; set; }
}
