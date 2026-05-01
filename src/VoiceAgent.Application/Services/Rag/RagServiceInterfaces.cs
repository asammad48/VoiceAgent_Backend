namespace VoiceAgent.Application.Services.Rag;

public interface IKnowledgeChunkingService
{
    IReadOnlyCollection<string> Chunk(string rawContent, int targetChunkTokens = 650, int overlapTokens = 100);
}

public interface IEmbeddingService
{
    Task<IReadOnlyCollection<float[]>> GenerateAsync(IReadOnlyCollection<string> chunks, CancellationToken cancellationToken = default);
    Task<float[]> GenerateQueryEmbeddingAsync(string query, CancellationToken cancellationToken = default);
}

public interface IRagRetrievalService
{
    Task<RagSearchResult> SearchAsync(RagSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IRagPromptContextService
{
    string BuildPromptContext(IReadOnlyCollection<RagChunkMatch> chunks, int maxChunks = 5);
}
