using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class SalesSeed
{
    public static readonly IReadOnlyList<OutboundLead> Leads =
    [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000901"), TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, CampaignId = SeedIds.MedicareCampaign, Name = "John Smith", Phone = "+447700900001", Email = "", DataJson = "{\"ageRange\":\"65-74\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000902"), TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, CampaignId = SeedIds.MedicareCampaign, Name = "Mary Johnson", Phone = "+447700900002", Email = "", DataJson = "{\"ageRange\":\"75+\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000903"), TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, CampaignId = SeedIds.MedicareCampaign, Name = "Robert Brown", Phone = "+447700900003", Email = "", DataJson = "{\"ageRange\":\"64 soon\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000904"), TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, CampaignId = SeedIds.MedicareCampaign, Name = "Patricia Davis", Phone = "+447700900004", Email = "", DataJson = "{\"ageRange\":\"Unknown\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000905"), TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, CampaignId = SeedIds.MedicareCampaign, Name = "Michael Wilson", Phone = "+447700900005", Email = "", DataJson = "{\"ageRange\":\"Unknown\"}", Status = "New" },

        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000911"), TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, CampaignId = SeedIds.AcaCampaign, Name = "Alex Green", Phone = "+447700900101", Email = "", DataJson = "{\"currentInsuranceStatus\":\"Unknown\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000912"), TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, CampaignId = SeedIds.AcaCampaign, Name = "Emma White", Phone = "+447700900102", Email = "", DataJson = "{\"currentInsuranceStatus\":\"Insured\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000913"), TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, CampaignId = SeedIds.AcaCampaign, Name = "Daniel Harris", Phone = "+447700900103", Email = "", DataJson = "{\"currentInsuranceStatus\":\"Uninsured\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000914"), TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, CampaignId = SeedIds.AcaCampaign, Name = "Sophie Martin", Phone = "+447700900104", Email = "", DataJson = "{\"currentInsuranceStatus\":\"Unknown\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000915"), TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, CampaignId = SeedIds.AcaCampaign, Name = "James Clark", Phone = "+447700900105", Email = "", DataJson = "{\"currentInsuranceStatus\":\"Insured\"}", Status = "New" },

        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000921"), TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, CampaignId = SeedIds.FeCampaign, Name = "A1 Builders Ltd", Phone = "+447700900201", Email = "", DataJson = "{\"businessType\":\"Construction\",\"monthlyRevenueRange\":\"20k-40k\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000922"), TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, CampaignId = SeedIds.FeCampaign, Name = "Bradford Desserts Ltd", Phone = "+447700900202", Email = "", DataJson = "{\"businessType\":\"Food\",\"monthlyRevenueRange\":\"10k-20k\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000923"), TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, CampaignId = SeedIds.FeCampaign, Name = "QuickFix Plumbing", Phone = "+447700900203", Email = "", DataJson = "{\"businessType\":\"Trade\",\"monthlyRevenueRange\":\"15k-30k\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000924"), TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, CampaignId = SeedIds.FeCampaign, Name = "Urban Retail Store", Phone = "+447700900204", Email = "", DataJson = "{\"businessType\":\"Retail\",\"monthlyRevenueRange\":\"30k-50k\"}", Status = "New" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000925"), TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, CampaignId = SeedIds.FeCampaign, Name = "NorthSide Logistics", Phone = "+447700900205", Email = "", DataJson = "{\"businessType\":\"Logistics\",\"monthlyRevenueRange\":\"40k+\"}", Status = "New" }
    ];
}
