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
        "HumanTransferSettingsJson",
        "LlmJson",
        "VoiceJson",
        "RagJson",
        "RecordingJson",
        "BillingJson",
        "FailureHandlingJson",
        "HumanTransferConfigurationJson"
    ];

    public static readonly string[] Indexes =
    [
        "TenantId",
        "ClientId",
        "CampaignId",
        "HumanTransferMode",
        "CreatedOn",
        "IsActive",
        "IsDeleted"
    ];
}
