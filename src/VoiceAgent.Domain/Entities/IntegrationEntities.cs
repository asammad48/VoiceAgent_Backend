using VoiceAgent.Common.Entities;

namespace VoiceAgent.Domain.Entities;

public class ToolDefinition : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ToolType { get; set; } = string.Empty;
    public string? RequiredSlotsJson { get; set; }
}

public class ToolCallLog : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string? RequestJson { get; set; }
    public string? ResponseJson { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int? DurationMs { get; set; }
    public string? CorrelationId { get; set; }
}

public class ExternalApiConfiguration : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string? AuthType { get; set; }
    public string? HeadersJson { get; set; }
    public string? EndpointsJson { get; set; }
    public string? SecretReferenceJson { get; set; }
    public string? RetryPolicyJson { get; set; }
    public bool IsEnabled { get; set; }
}


public class ExternalSystemLog : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string EndpointKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string? RequestJson { get; set; }
    public string? ResponseJson { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
}
