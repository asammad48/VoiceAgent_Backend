using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class CourierSeed
{
    public static readonly Guid ProfileId = Guid.Parse("20000000-0000-0000-0000-000000000301");
    public static readonly IReadOnlyList<CourierPricingProfile> Profiles = [new() { Id = ProfileId, TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, BranchId = SeedIds.CourierBranch, Name = "QuickMove Standard Pricing", Currency = "GBP", BaseFee = 4.00m, PricePerKm = 1.10m, PricePerKg = 0.75m, MinimumFee = 7.00m, MaxDistanceKm = 50m, SettingsJson = "{\"standardDeliveryHours\":\"3-5 hours\",\"sameDayDeliveryHours\":\"1-3 hours\",\"fragilePackageExtraFee\":2.50,\"urgentMultiplier\":1.35}", IsActive = true }];
    public static readonly IReadOnlyList<CourierDistanceBand> DistanceBands = [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000311"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CourierPricingProfileId = ProfileId, FromKm = 0, ToKm = 5, Fee = 3 },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000312"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CourierPricingProfileId = ProfileId, FromKm = 5, ToKm = 15, Fee = 8 },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000313"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CourierPricingProfileId = ProfileId, FromKm = 15, ToKm = 30, Fee = 15 },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000314"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CourierPricingProfileId = ProfileId, FromKm = 30, ToKm = 50, Fee = 25 }
    ];
    public static readonly IReadOnlyList<CourierWeightBand> WeightBands = [new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000321"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CourierPricingProfileId = ProfileId, FromKg = 0, ToKg = 2, Fee = 1 },new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000322"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CourierPricingProfileId = ProfileId, FromKg = 2, ToKg = 5, Fee = 3 }];
    public static readonly IReadOnlyList<CourierZone> Zones = [new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000331"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CourierPricingProfileId = ProfileId, Name = "Bradford Local Zone", ZoneJson = "{\"city\":\"Bradford\"}", ExtraFee = 0, IsActive = true },new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000332"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CourierPricingProfileId = ProfileId, Name = "Leeds Zone", ZoneJson = "{\"city\":\"Leeds\"}", ExtraFee = 5, IsActive = true },new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000333"), TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CourierPricingProfileId = ProfileId, Name = "Manchester Zone", ZoneJson = "{\"city\":\"Manchester\"}", ExtraFee = 15, IsActive = true }];
}
