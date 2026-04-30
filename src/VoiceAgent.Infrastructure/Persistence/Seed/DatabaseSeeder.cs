using Microsoft.EntityFrameworkCore;
using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static readonly Guid DemoTenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid DemoRestaurantClientId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid DemoCourierClientId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid RestaurantCampaignId = Guid.Parse("10000000-0000-0000-0000-000000000010");
    public static readonly Guid CourierCampaignId = Guid.Parse("10000000-0000-0000-0000-000000000011");

    public static async Task SeedAsync(AppDbContext dbContext)
    {
        await dbContext.Database.MigrateAsync();
        if (await dbContext.Tenants.AnyAsync()) return;

        var branchId = Guid.Parse("10000000-0000-0000-0000-000000000020");
        var menuId = Guid.Parse("10000000-0000-0000-0000-000000000030");
        var catId = Guid.Parse("10000000-0000-0000-0000-000000000031");
        var itemId = Guid.Parse("10000000-0000-0000-0000-000000000032");

        dbContext.Tenants.Add(new Tenant { Id = DemoTenantId, Name = "Demo Tenant", Slug = "demo", DefaultCurrency = "USD", DefaultTimezone = "UTC" });
        dbContext.Clients.AddRange(
            new Client { Id = DemoRestaurantClientId, TenantId = DemoTenantId, Name = "Demo Restaurant", AgentName = "Maya", IndustryType = "restaurant" },
            new Client { Id = DemoCourierClientId, TenantId = DemoTenantId, Name = "Demo Courier", AgentName = "Sam", IndustryType = "courier" });

        dbContext.Branches.AddRange(
            new Branch { Id = branchId, TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, Name = "Main Branch", Timezone = "UTC" },
            new Branch { Id = Guid.Parse("10000000-0000-0000-0000-000000000021"), TenantId = DemoTenantId, ClientId = DemoCourierClientId, Name = "Hub", Timezone = "UTC" });

        dbContext.Campaigns.AddRange(
            new Campaign { Id = RestaurantCampaignId, TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, Name = "Restaurant Demo", CampaignType = CampaignType.RestaurantOrder, Direction = CampaignDirection.Inbound, IsDemoEnabled = true, IsActive = true },
            new Campaign { Id = CourierCampaignId, TenantId = DemoTenantId, ClientId = DemoCourierClientId, Name = "Courier Demo", CampaignType = CampaignType.CourierService, Direction = CampaignDirection.Inbound, IsDemoEnabled = true, IsActive = true },
            new Campaign { Id = Guid.Parse("10000000-0000-0000-0000-000000000012"), TenantId = DemoTenantId, ClientId = DemoCourierClientId, Name = "Cab Demo", CampaignType = CampaignType.CabBooking, Direction = CampaignDirection.Inbound, IsDemoEnabled = true, IsActive = true },
            new Campaign { Id = Guid.Parse("10000000-0000-0000-0000-000000000013"), TenantId = DemoTenantId, ClientId = DemoCourierClientId, Name = "Doctor Demo", CampaignType = CampaignType.DoctorAppointment, Direction = CampaignDirection.Inbound, IsDemoEnabled = true, IsActive = true },
            new Campaign { Id = Guid.Parse("10000000-0000-0000-0000-000000000014"), TenantId = DemoTenantId, ClientId = DemoCourierClientId, Name = "Sales Demo", CampaignType = CampaignType.MedicareSales, Direction = CampaignDirection.Outbound, IsDemoEnabled = true, IsActive = true });

        dbContext.CampaignConfigurations.AddRange(
            new CampaignConfiguration { Id = Guid.Parse("10000000-0000-0000-0000-000000000015"), TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, CampaignId = RestaurantCampaignId, RequiredSlotsJson = "[]", AllowedToolsJson = "[]", IsActive = true },
            new CampaignConfiguration { Id = Guid.Parse("10000000-0000-0000-0000-000000000016"), TenantId = DemoTenantId, ClientId = DemoCourierClientId, CampaignId = CourierCampaignId, RequiredSlotsJson = "[]", AllowedToolsJson = "[]", IsActive = true });

        dbContext.PromptVersions.Add(new PromptVersion { Id = Guid.Parse("10000000-0000-0000-0000-000000000017"), TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, CampaignId = RestaurantCampaignId, Name = "Default", Version = 1, SystemPrompt = "You are a helpful agent.", IsActive = true });
        dbContext.RestaurantMenus.Add(new RestaurantMenu { Id = menuId, TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, Name = "Main Menu", IsActive = true });
        dbContext.MenuCategories.Add(new MenuCategory { Id = catId, TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, MenuId = menuId, Name = "Burgers", SortOrder = 1, IsActive = true });
        dbContext.MenuItems.Add(new MenuItem { Id = itemId, TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, MenuId = menuId, CategoryId = catId, Name = "Classic Burger", Description = "", BasePrice = 8.99m, Currency = "USD", IsAvailable = true, IsActive = true });
        dbContext.MenuItemAddons.Add(new MenuItemAddon { Id = Guid.Parse("10000000-0000-0000-0000-000000000033"), TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, MenuItemId = itemId, Name = "Cheese", PriceDelta = 1, IsDefault = false });
        dbContext.RestaurantDeals.Add(new RestaurantDeal { Id = Guid.Parse("10000000-0000-0000-0000-000000000034"), TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, Name = "Lunch Combo", Description = "", DealPrice = 10.99m, Currency = "USD", IsAvailable = true, IsActive = true });
        dbContext.CourierPricingProfiles.Add(new CourierPricingProfile { Id = Guid.Parse("10000000-0000-0000-0000-000000000035"), TenantId = DemoTenantId, ClientId = DemoCourierClientId, Name = "Standard", Currency = "USD", BaseFee = 4, PricePerKm = 1.2m, PricePerKg = 0.8m, MinimumFee = 6, IsActive = true });

        var kbId = Guid.Parse("10000000-0000-0000-0000-000000000040");
        var docId = Guid.Parse("10000000-0000-0000-0000-000000000041");
        dbContext.KnowledgeBases.Add(new KnowledgeBase { Id = kbId, TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, Name = "Restaurant KB", IsActive = true });
        dbContext.KnowledgeDocuments.Add(new KnowledgeDocument { Id = docId, TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, KnowledgeBaseId = kbId, Title = "FAQ", SourceType = "seed", IsActive = true });
        dbContext.KnowledgeChunks.Add(new KnowledgeChunk { Id = Guid.Parse("10000000-0000-0000-0000-000000000042"), TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, CampaignId = RestaurantCampaignId, KnowledgeBaseId = kbId, KnowledgeDocumentId = docId, ChunkText = "We open at 9 AM.", EmbeddingJson = "[]", MetadataJson = "{}", IsActive = true });
        dbContext.ExternalApiConfigurations.Add(new ExternalApiConfiguration { Id = Guid.Parse("10000000-0000-0000-0000-000000000043"), TenantId = DemoTenantId, ClientId = DemoRestaurantClientId, CampaignId = RestaurantCampaignId, Name = "Disabled", BaseUrl = "https://example.com", AuthType = "none", HeadersJson = "{}", EndpointsJson = "{}", SecretReferenceJson = "{}", IsEnabled = false });

        await dbContext.SaveChangesAsync();
    }
}
