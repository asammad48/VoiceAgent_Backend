using VoiceAgent.Domain.Entities;
using System.Linq;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class RagSeed
{
    public static readonly IReadOnlyList<KnowledgeBase> Bases = [
        MakeKb(Guid.Parse("20000000-0000-0000-0000-000000000501"), SeedIds.RestaurantClient, SeedIds.RestaurantCampaign, "Restaurant KB"),
        MakeKb(Guid.Parse("20000000-0000-0000-0000-000000000502"), SeedIds.CourierClient, SeedIds.CourierCampaign, "Courier KB"),
        MakeKb(Guid.Parse("20000000-0000-0000-0000-000000000503"), SeedIds.CabClient, SeedIds.CabCampaign, "Cab KB"),
        MakeKb(Guid.Parse("20000000-0000-0000-0000-000000000504"), SeedIds.DoctorClient, SeedIds.DoctorCampaign, "Doctor KB"),
        MakeKb(Guid.Parse("20000000-0000-0000-0000-000000000505"), SeedIds.MedicareClient, SeedIds.MedicareCampaign, "Medicare KB"),
        MakeKb(Guid.Parse("20000000-0000-0000-0000-000000000506"), SeedIds.AcaClient, SeedIds.AcaCampaign, "ACA KB"),
        MakeKb(Guid.Parse("20000000-0000-0000-0000-000000000507"), SeedIds.FeClient, SeedIds.FeCampaign, "FE KB")
    ];

    public static readonly IReadOnlyList<KnowledgeDocument> Documents = Bases.SelectMany((b, idx) => new[]
    {
        MakeDoc(Guid.Parse($"20000000-0000-0000-0000-000000000{600 + idx * 3 + 1}"), b, "FAQ", "FAQ", "Demo FAQ content"),
        MakeDoc(Guid.Parse($"20000000-0000-0000-0000-000000000{600 + idx * 3 + 2}"), b, "Policy", "Policy", "Demo policy content"),
        MakeDoc(Guid.Parse($"20000000-0000-0000-0000-000000000{600 + idx * 3 + 3}"), b, "Script", "Script", "Demo script content")
    }).ToList();

    public static readonly IReadOnlyList<KnowledgeChunk> Chunks = Documents.Select((d, i) => new KnowledgeChunk
    {
        Id = Guid.Parse($"20000000-0000-0000-0000-000000000{700 + i}"),
        TenantId = d.TenantId,
        ClientId = d.ClientId,
        CampaignId = d.CampaignId,
        KnowledgeBaseId = d.KnowledgeBaseId,
        KnowledgeDocumentId = d.Id,
        ChunkText = BuildChunkText(d),
        EmbeddingJson = "[]",
        MetadataJson = $"{{\"source\":\"seed\",\"documentType\":\"{d.DocumentType}\"}}",
        IsActive = true
    }).ToList();

    private static KnowledgeBase MakeKb(Guid id, Guid clientId, Guid campaignId, string name) => new() { Id = id, TenantId = SeedIds.Tenant, ClientId = clientId, CampaignId = campaignId, Name = name, Description = "Seeded knowledge base", IsActive = true };
    private static KnowledgeDocument MakeDoc(Guid id, KnowledgeBase kb, string title, string type, string content) => new() { Id = id, TenantId = kb.TenantId, ClientId = kb.ClientId, CampaignId = kb.CampaignId, KnowledgeBaseId = kb.Id, Title = title, DocumentType = type, Content = content, MetadataJson = "{\"source\":\"seed\"}", IsActive = true };

    private static string BuildChunkText(KnowledgeDocument document)
    {
        if (document.CampaignId == SeedIds.RestaurantCampaign)
            return document.DocumentType == "Policy"
                ? "Delivery within 8 km, minimum order £10, demo payment method is cash on delivery."
                : document.DocumentType == "Script"
                    ? "Offer menu categories first, then deals or specific item help."
                    : "We can help with burgers, pizza, fries, drinks, desserts, and meal deals.";
        if (document.CampaignId == SeedIds.CourierCampaign)
            return document.DocumentType == "Policy"
                ? "Do not transport illegal items, dangerous chemicals, weapons, or unsealed liquids."
                : document.DocumentType == "Script"
                    ? "Collect pickup, dropoff, weight, package type, and urgency before quoting."
                    : "Courier supports documents, parcels, and small business deliveries.";
        if (document.CampaignId == SeedIds.DoctorCampaign)
            return "This clinic handles non-emergency appointments only; emergency symptoms require urgent care.";
        if (document.CampaignId == SeedIds.CabCampaign)
            return "Cab fare is an estimate until booking confirmation; accessible and 6-seater vehicles depend on availability.";
        if (document.CampaignId == SeedIds.MedicareCampaign)
            return "Do not claim government affiliation or guaranteed savings; capture interest and opt-out requests.";
        if (document.CampaignId == SeedIds.AcaCampaign)
            return "Do not determine eligibility or subsidies; capture details for licensed follow-up.";
        return "Do not promise funding approval or rates; capture enquiry and callback preferences.";
    }
}
