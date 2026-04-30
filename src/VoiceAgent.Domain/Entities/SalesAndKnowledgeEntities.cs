using VoiceAgent.Common.Entities;

namespace VoiceAgent.Domain.Entities;

public class OutboundLead : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? LeadDataJson { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
}

public class OutboundCampaignRun : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StatsJson { get; set; }
}

public class OutboundAttempt : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid LeadId { get; set; }
    public Guid? CallSessionId { get; set; }
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Disposition { get; set; }
}

public class KnowledgeBase : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class KnowledgeDocument : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? SourceFileName { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public int Version { get; set; }
}

public class KnowledgeChunk : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public Guid KnowledgeDocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public string? EmbeddingVector { get; set; }
    public string? MetadataJson { get; set; }
}

public class CallCostLog : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public long LlmInputTokens { get; set; }
    public long LlmOutputTokens { get; set; }
    public long TtsCharacters { get; set; }
    public decimal SttAudioSeconds { get; set; }
    public int CallDurationSeconds { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? CorrelationId { get; set; }
}

public class TenantUsageMonthly : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalCalls { get; set; }
    public int TotalMinutes { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public bool WarningExceeded { get; set; }
    public bool IsBlockedManually { get; set; }
}
