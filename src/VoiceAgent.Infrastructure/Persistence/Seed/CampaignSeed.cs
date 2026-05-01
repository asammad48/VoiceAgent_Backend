using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class CampaignSeed
{
    public static readonly IReadOnlyList<Campaign> All =
    [
        new() { Id = SeedIds.RestaurantCampaign, TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, BranchId = SeedIds.RestaurantBranch, Name = "Restaurant Order Demo", CampaignType = CampaignType.RestaurantOrder, Direction = CampaignDirection.WebDemo, IsDemoEnabled = true },
        new() { Id = SeedIds.CourierCampaign, TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, BranchId = SeedIds.CourierBranch, Name = "Courier Service Demo", CampaignType = CampaignType.CourierService, Direction = CampaignDirection.WebDemo, IsDemoEnabled = true },
        new() { Id = SeedIds.CabCampaign, TenantId = SeedIds.Tenant, ClientId = SeedIds.CabClient, BranchId = SeedIds.CabBranch, Name = "Cab Booking Demo", CampaignType = CampaignType.CabBooking, Direction = CampaignDirection.WebDemo, IsDemoEnabled = true },
        new() { Id = SeedIds.DoctorCampaign, TenantId = SeedIds.Tenant, ClientId = SeedIds.DoctorClient, BranchId = SeedIds.DoctorBranch, Name = "Doctor Appointment Demo", CampaignType = CampaignType.DoctorAppointment, Direction = CampaignDirection.WebDemo, IsDemoEnabled = true },
        new() { Id = SeedIds.MedicareCampaign, TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, BranchId = SeedIds.MedicareSalesBranch, Name = "Medicare Sales Demo", CampaignType = CampaignType.MedicareSales, Direction = CampaignDirection.Outbound, IsDemoEnabled = true },
        new() { Id = SeedIds.AcaCampaign, TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, BranchId = SeedIds.AcaSalesBranch, Name = "ACA Sales Demo", CampaignType = CampaignType.AcaSales, Direction = CampaignDirection.Outbound, IsDemoEnabled = true },
        new() { Id = SeedIds.FeCampaign, TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, BranchId = SeedIds.FeSalesBranch, Name = "FE Sales Demo", CampaignType = CampaignType.FeSales, Direction = CampaignDirection.Outbound, IsDemoEnabled = true }
    ];
}
