namespace VoiceAgent.WorkerService.Workers;

public class KnowledgeIndexingWorker
{
    // Production indexing flow:
    // Upload document/text
    //  -> Create KnowledgeDocument
    //  -> Chunk content (500-800 tokens, overlap 80-120)
    //  -> Generate embeddings
    //  -> Save KnowledgeChunks
    //  -> Mark document indexed
    //
    // Scope filters required in all retrieval/indexing operations:
    // TenantId, ClientId, CampaignId, KnowledgeBaseId, IsActive.
}
