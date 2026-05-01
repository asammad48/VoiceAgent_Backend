using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class ClientSeed
{
    public static readonly IReadOnlyList<Client> All =
    [
        new() { Id = SeedIds.RestaurantClient, TenantId = SeedIds.Tenant, Name = "Demo Restaurant Client", IndustryType = "Restaurant", AgentName = "Maya", CallRecordingEnabled = true },
        new() { Id = SeedIds.CourierClient, TenantId = SeedIds.Tenant, Name = "Demo Courier Client", IndustryType = "Courier", AgentName = "Sam", CallRecordingEnabled = true },
        new() { Id = SeedIds.CabClient, TenantId = SeedIds.Tenant, Name = "Demo Cab Client", IndustryType = "CabService", AgentName = "Adam", CallRecordingEnabled = true },
        new() { Id = SeedIds.DoctorClient, TenantId = SeedIds.Tenant, Name = "Demo Doctor Client", IndustryType = "Healthcare", AgentName = "Sara", CallRecordingEnabled = false },
        new() { Id = SeedIds.MedicareClient, TenantId = SeedIds.Tenant, Name = "Demo Medicare Sales Client", IndustryType = "Sales", AgentName = "Olivia", CallRecordingEnabled = true },
        new() { Id = SeedIds.AcaClient, TenantId = SeedIds.Tenant, Name = "Demo ACA Sales Client", IndustryType = "Sales", AgentName = "Noah", CallRecordingEnabled = true },
        new() { Id = SeedIds.FeClient, TenantId = SeedIds.Tenant, Name = "Demo FE Sales Client", IndustryType = "Sales", AgentName = "Emma", CallRecordingEnabled = true }
    ];
}
