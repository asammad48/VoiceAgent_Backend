namespace VoiceAgent.Infrastructure.Persistence.Configurations;

public static class KnowledgeChunkConfiguration
{
    public const string Entity = "KnowledgeChunk";
    public static readonly string[] Indexes =
    [
        "TenantId",
        "ClientId",
        "CampaignId",
        "KnowledgeBaseId",
        "CreatedOn",
        "IsActive"
    ];

    public const string VectorIndexNote = "Create vector index on EmbeddingVector (pgvector) during migration.";
}
