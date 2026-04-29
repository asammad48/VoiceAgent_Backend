namespace VoiceAgent.Infrastructure.Persistence.Configurations;

public static class ClientConfiguration
{
    public const string Entity = "Client";
    public static readonly string[] JsonbColumns = ["SettingsJson"];
    public static readonly string[] Indexes = ["TenantId", "CreatedOn", "IsActive", "IsDeleted", "Status"];
}
