namespace VoiceAgent.Infrastructure.Persistence.Configurations;

public static class CampaignConfigurationEntityConfiguration
{
    public const string Entity = "CampaignConfiguration";
    public static readonly string[] JsonbColumns =
    [
        "RequiredSlotsJson",
        "OptionalSlotsJson",
        "AllowedToolsJson",
        "ValidationRulesJson",
        "FallbackRulesJson",
        "ConfirmationRulesJson",
        "HumanTransferJson",
        "LlmSettingsJson",
        "VoiceSettingsJson",
        "RagSettingsJson",
        "RecordingSettingsJson",
        "BillingSettingsJson"
    ];
    public static readonly string[] Indexes = ["TenantId", "ClientId", "CampaignId", "CreatedOn", "IsActive", "IsDeleted"];
}
