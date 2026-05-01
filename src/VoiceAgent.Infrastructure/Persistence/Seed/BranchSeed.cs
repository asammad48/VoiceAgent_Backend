using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class BranchSeed
{
    public static readonly IReadOnlyList<Branch> All =
    [
        new() { Id = SeedIds.RestaurantBranch, TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, Name = "Belgravia Demo Kitchen", Address = "20 Market Street, Bradford, BD1 1LH, UK", Latitude = 53.795984m, Longitude = -1.759398m, DeliveryRadiusKm = 8m, DeliveryFeeRulesJson = "[{\"fromKm\":0,\"toKm\":2,\"fee\":1.50},{\"fromKm\":2,\"toKm\":5,\"fee\":2.99},{\"fromKm\":5,\"toKm\":8,\"fee\":4.99}]", BusinessHoursJson = "{\"monday\":\"16:00-02:00\",\"tuesday\":\"16:00-02:00\",\"wednesday\":\"16:00-02:00\",\"thursday\":\"16:00-02:00\",\"friday\":\"16:00-03:00\",\"saturday\":\"16:00-03:00\",\"sunday\":\"16:00-02:00\"}" },
        new() { Id = SeedIds.CourierBranch, TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, Name = "QuickMove Bradford Hub", Address = "Bradford City Centre, UK", Latitude = 53.795984m, Longitude = -1.759398m, DeliveryRadiusKm = 50m, BusinessHoursJson = "{\"monday\":\"08:00-20:00\",\"tuesday\":\"08:00-20:00\",\"wednesday\":\"08:00-20:00\",\"thursday\":\"08:00-20:00\",\"friday\":\"08:00-20:00\",\"saturday\":\"09:00-18:00\",\"sunday\":\"10:00-16:00\"}" },
        new() { Id = SeedIds.CabBranch, TenantId = SeedIds.Tenant, ClientId = SeedIds.CabClient, Name = "CityRide Bradford Office", Address = "Bradford Interchange, Bradford, UK", Latitude = 53.7914m, Longitude = -1.7491m, DeliveryRadiusKm = 40m },
        new() { Id = SeedIds.DoctorBranch, TenantId = SeedIds.Tenant, ClientId = SeedIds.DoctorClient, Name = "CareFirst Demo Clinic", Address = "10 Health Road, Bradford, UK", Latitude = 53.8008m, Longitude = -1.7590m },
        new() { Id = SeedIds.MedicareSalesBranch, TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, Name = "Remote Sales Desk", Address = "Remote" },
        new() { Id = SeedIds.AcaSalesBranch, TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, Name = "Remote Sales Desk", Address = "Remote" },
        new() { Id = SeedIds.FeSalesBranch, TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, Name = "Remote Sales Desk", Address = "Remote" }
    ];
}
