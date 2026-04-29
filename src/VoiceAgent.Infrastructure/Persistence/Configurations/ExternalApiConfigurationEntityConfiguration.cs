namespace VoiceAgent.Infrastructure.Persistence.Configurations;

public static class ExternalApiConfigurationEntityConfiguration
{
    public const string Entity = "ExternalApiConfiguration";
    public static readonly string[] JsonbColumns = ["HeadersJson", "EndpointsJson", "SecretReferenceJson", "RetryPolicyJson"];
    public static readonly string[] Indexes = ["TenantId", "ClientId", "CampaignId", "CreatedOn", "IsActive", "IsDeleted"];
}
