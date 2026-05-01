using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class PromptSeed
{
    public static readonly IReadOnlyList<PromptVersion> All =
    [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000201"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, CampaignId = SeedIds.RestaurantCampaign, Name = "Default", Version = 1, SystemPrompt = "You are Maya, a helpful restaurant ordering assistant. Help customers browse menu categories, ask about deals, add items, choose addons, calculate totals using tools, and capture orders. Keep replies short. Never invent prices. Use database menu and pricing only." },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000202"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CampaignId = SeedIds.CourierCampaign, Name = "Default", Version = 1, SystemPrompt = "You are Sam, a courier booking assistant. Help customers provide pickup, dropoff, weight, package type, and urgency. Use routing and pricing tools for estimates. Never invent delivery price or time." },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000203"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CabClient, CampaignId = SeedIds.CabCampaign, Name = "Default", Version = 1, SystemPrompt = "You are Adam, a cab booking assistant. Help customers provide pickup, dropoff, time, passenger count, and vehicle type. Give only estimated fare from tools or configured rules." },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000204"), TenantId = SeedIds.Tenant, ClientId = SeedIds.DoctorClient, CampaignId = SeedIds.DoctorCampaign, Name = "Default", Version = 1, SystemPrompt = "You are Sara, a clinic appointment assistant. Capture appointment requests. Do not diagnose. For emergency symptoms, advise urgent/emergency care and follow handoff rules." },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000205"), TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, CampaignId = SeedIds.MedicareCampaign, Name = "Default", Version = 1, SystemPrompt = "You are Olivia, a polite sales assistant for Medicare-related information. Do not claim eligibility, savings, or government affiliation. Capture interest and opt-out requests." },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000206"), TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, CampaignId = SeedIds.AcaCampaign, Name = "Default", Version = 1, SystemPrompt = "You are Noah, a polite health coverage enquiry assistant. Do not determine eligibility or pricing. Capture basic information and interest." },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000207"), TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, CampaignId = SeedIds.FeCampaign, Name = "Default", Version = 1, SystemPrompt = "You are Emma, a business funding enquiry assistant. Do not promise approval, rates, or funding. Capture enquiry details and callback requests." }
    ];
}
