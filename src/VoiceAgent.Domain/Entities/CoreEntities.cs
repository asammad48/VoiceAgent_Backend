using VoiceAgent.Common.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Domain.Entities;

public class Tenant : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DefaultTimezone { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = string.Empty;
    public string? SettingsJson { get; set; }
}

public class Client : AuditableEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string IndustryType { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? SettingsJson { get; set; }
    public bool CallRecordingEnabled { get; set; }
}

public class Branch : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? DeliveryRadiusKm { get; set; }
    public string? BusinessHoursJson { get; set; }
    public string? DeliveryFeeRulesJson { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
}

public class CampaignTemplate : AuditableEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CampaignType CampaignType { get; set; }
    public CampaignDirection Direction { get; set; }
    public string? Description { get; set; }
    public string? BaseFlowJson { get; set; }
}

public class Campaign : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CampaignTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public CampaignType CampaignType { get; set; }
    public CampaignDirection Direction { get; set; }
    public bool IsDemoEnabled { get; set; }
    public bool IsActive { get; set; }
}

public class CampaignConfiguration : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public string? RequiredSlotsJson { get; set; }
    public string? OptionalSlotsJson { get; set; }
    public string? AllowedToolsJson { get; set; }
    public string? ValidationRulesJson { get; set; }
    public string? FallbackRulesJson { get; set; }
    public string? ConfirmationRulesJson { get; set; }
    public string? HumanTransferSettingsJson { get; set; }
    public HumanTransferMode? HumanTransferMode { get; set; }
    public string? LlmJson { get; set; }
    public string? VoiceJson { get; set; }
    public string? RagJson { get; set; }
    public string? RecordingJson { get; set; }
    public string? BillingJson { get; set; }
    public string? FailureHandlingJson { get; set; }
    public string? HumanTransferConfigurationJson { get; set; }
}

