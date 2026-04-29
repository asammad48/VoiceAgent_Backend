namespace VoiceAgent.Infrastructure.Persistence.Configurations;

public static class TenantConfiguration
{
    public const string Entity = "Tenant";
    public static readonly string[] JsonbColumns = ["SettingsJson"];
    public static readonly string[] Indexes = ["CreatedOn", "IsActive", "IsDeleted", "Status"];
    public const string Example = "builder.Property(x => x.SettingsJson).HasColumnType(\"jsonb\");";
}
