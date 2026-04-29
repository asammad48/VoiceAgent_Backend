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
        "IsActive",
        "CreatedOn"
    ];

    public const string VectorIndexNote =
        "Use PostgreSQL + pgvector. Create a vector index on EmbeddingVector with provider dimension (e.g., 1536).";
}
