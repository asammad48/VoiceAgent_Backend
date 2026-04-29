namespace VoiceAgent.Infrastructure.Persistence.Configurations;

public static class EfCorePostgresGuidelines
{
    public const string WhyPostgres = "Relational core data + JSONB flexibility + pgvector for RAG + strong filtering/reporting.";
    public const string JsonFastApproach = "Use string JSON properties initialized to '{}' for implementation speed.";
    public const string MultiTenantRule = "Do not rely only on global filters. Always pass TenantId and ClientId in Application queries.";
}
