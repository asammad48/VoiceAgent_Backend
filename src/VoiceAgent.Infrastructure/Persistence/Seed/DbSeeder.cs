using Microsoft.EntityFrameworkCore;
using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public class DbSeeder(AppDbContext db)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Tenants.AnyAsync(cancellationToken)) return;

        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var clientId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var campaignId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Demo Tenant", Slug = "demo-tenant", DefaultTimezone = "UTC", DefaultCurrency = "USD" });
        db.Clients.Add(new Client { Id = clientId, TenantId = tenantId, Name = "Demo Restaurant+Courier", IndustryType = "multi" });
        db.Campaigns.Add(new Campaign { Id = campaignId, TenantId = tenantId, ClientId = clientId, Name = "Restaurant and Courier", CampaignType = CampaignType.RestaurantOrder, Direction = CampaignDirection.Inbound, IsActive = true, IsDemoEnabled = true });
        db.MenuItems.AddRange(
            new MenuItem { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = clientId, MenuId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Name = "Classic Burger", Currency = "USD", BasePrice = 8.99m, IsAvailable = true },
            new MenuItem { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = clientId, MenuId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Name = "Chicken Pizza", Currency = "USD", BasePrice = 12.50m, IsAvailable = true });
        db.RestaurantDeals.Add(new RestaurantDeal { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = clientId, Name = "Lunch Combo", Currency = "USD", DealPrice = 10.99m, IsAvailable = true });
        db.CourierPricingProfiles.Add(new CourierPricingProfile { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = clientId, Name = "Standard", Currency = "USD", BaseFee = 4, PricePerKm = 1.2m, PricePerKg = 0.8m, MinimumFee = 6 });
        await db.SaveChangesAsync(cancellationToken);
    }
}
