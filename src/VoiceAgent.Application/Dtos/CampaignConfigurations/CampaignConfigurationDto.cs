namespace VoiceAgent.Application.Dtos.CampaignConfigurations;

public class CreateCampaignConfigurationRequestDto
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
    public string? LlmSettingsJson { get; set; }
    public string? VoiceSettingsJson { get; set; }
    public string? RagSettingsJson { get; set; }
    public string? HumanTransferJson { get; set; }
}

public class CampaignConfigurationResponseDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string? RequiredSlotsJson { get; set; }
    public string? OptionalSlotsJson { get; set; }
    public string? AllowedToolsJson { get; set; }
    public string? ValidationRulesJson { get; set; }
    public string? FallbackRulesJson { get; set; }
    public string? ConfirmationRulesJson { get; set; }
    public string? LlmSettingsJson { get; set; }
    public string? VoiceSettingsJson { get; set; }
    public string? RagSettingsJson { get; set; }
    public string? HumanTransferJson { get; set; }
    public bool IsActive { get; set; }
}
