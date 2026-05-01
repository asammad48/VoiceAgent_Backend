using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;

namespace VoiceAgent.Application.Services.Rag;

public sealed class DbRagRetrievalService(IAppDbContext db) : IRagRetrievalService
{
    public async Task<RagSearchResult> SearchAsync(RagSearchRequest request, CancellationToken cancellationToken = default)
    {
        var q = request.UserQuery.Trim();
        if (string.IsNullOrWhiteSpace(q)) return new RagSearchResult(false, Array.Empty<RagChunkMatch>(), "EmptyQuery");

        var chunks = await db.KnowledgeChunks
            .Where(x => x.TenantId == request.Scope.TenantId
                     && x.ClientId == request.Scope.ClientId
                     && x.CampaignId == request.Scope.CampaignId
                     && x.KnowledgeBaseId == request.Scope.KnowledgeBaseId
                     && x.IsActive == request.Scope.IsActive)
            .OrderBy(x => x.ChunkIndex)
            .Take(200)
            .ToListAsync(cancellationToken);

        var terms = q.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToArray();
        var ranked = chunks
            .Select(c =>
            {
                var text = c.TextContent?.ToLowerInvariant() ?? string.Empty;
                var hits = terms.Count(t => text.Contains(t));
                var score = terms.Length == 0 ? 0m : (decimal)hits / terms.Length;
                return new { c, score };
            })
            .Where(x => x.score >= request.MinScore)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.c.ChunkIndex)
            .Take(request.TopK)
            .Select(x => new RagChunkMatch(x.c.Id, x.c.KnowledgeDocumentId, x.c.ChunkIndex, x.score, x.c.TextContent, x.c.DocumentType, x.c.MetadataJson))
            .ToList();

        return new RagSearchResult(ranked.Count > 0, ranked, ranked.Count > 0 ? null : "NoScopedMatch");
    }
}
