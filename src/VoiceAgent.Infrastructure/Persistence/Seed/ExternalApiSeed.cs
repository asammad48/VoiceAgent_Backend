using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class ExternalApiSeed
{
    public static readonly IReadOnlyList<ExternalApiConfiguration> All = [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000801"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, CampaignId = SeedIds.RestaurantCampaign, Name = "Demo Restaurant POS API", BaseUrl = "https://example-restaurant-pos.local", AuthType = "ApiKey", HeadersJson = "{\"Authorization\":\"Bearer {{SECRET:RestaurantPosToken}}\"}", EndpointsJson = "{\"createOrder\":\"/orders\",\"getMenu\":\"/menu\"}", SecretReferenceJson = "{}", IsEnabled = false },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000802"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CampaignId = SeedIds.CourierCampaign, Name = "Demo Courier Dispatch API", BaseUrl = "https://example-courier-dispatch.local", AuthType = "ApiKey", HeadersJson = "{}", EndpointsJson = "{\"createBooking\":\"/bookings\",\"getQuote\":\"/quotes\"}", SecretReferenceJson = "{}", IsEnabled = false },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000803"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CabClient, CampaignId = SeedIds.CabCampaign, Name = "Demo Cab Dispatch API", BaseUrl = "https://example-cab-dispatch.local", AuthType = "ApiKey", HeadersJson = "{}", EndpointsJson = "{}", SecretReferenceJson = "{}", IsEnabled = false },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000804"), TenantId = SeedIds.Tenant, ClientId = SeedIds.DoctorClient, CampaignId = SeedIds.DoctorCampaign, Name = "Demo Clinic Appointment API", BaseUrl = "https://example-clinic.local", AuthType = "ApiKey", HeadersJson = "{}", EndpointsJson = "{}", SecretReferenceJson = "{}", IsEnabled = false },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000805"), TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, CampaignId = SeedIds.MedicareCampaign, Name = "Demo CRM API", BaseUrl = "https://example-crm.local", AuthType = "ApiKey", HeadersJson = "{}", EndpointsJson = "{}", SecretReferenceJson = "{}", IsEnabled = false },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000806"), TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, CampaignId = SeedIds.AcaCampaign, Name = "Demo CRM API", BaseUrl = "https://example-crm.local", AuthType = "ApiKey", HeadersJson = "{}", EndpointsJson = "{}", SecretReferenceJson = "{}", IsEnabled = false },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000807"), TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, CampaignId = SeedIds.FeCampaign, Name = "Demo CRM API", BaseUrl = "https://example-crm.local", AuthType = "ApiKey", HeadersJson = "{}", EndpointsJson = "{}", SecretReferenceJson = "{}", IsEnabled = false }
    ];
}
