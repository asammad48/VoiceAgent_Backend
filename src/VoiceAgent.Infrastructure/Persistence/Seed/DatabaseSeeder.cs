using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, ILogger? logger = null)
    {
        logger?.LogInformation("Seed started");
        await dbContext.Database.MigrateAsync();

        var exists = await dbContext.Tenants.AnyAsync(t => t.Slug == TenantSeed.DemoTenant.Slug);
        if (exists)
        {
            logger?.LogInformation("Seed skipped because demo data already exists");
            return;
        }

        dbContext.Tenants.Add(TenantSeed.DemoTenant);
        dbContext.Clients.AddRange(ClientSeed.All);
        dbContext.Branches.AddRange(BranchSeed.All);
        dbContext.Campaigns.AddRange(CampaignSeed.All);
        dbContext.CampaignConfigurations.AddRange(CampaignConfigurationSeed.All);
        dbContext.PromptVersions.AddRange(PromptSeed.All);

        dbContext.RestaurantMenus.AddRange(RestaurantSeed.Menus);
        dbContext.MenuCategories.AddRange(RestaurantSeed.Categories);
        dbContext.MenuItems.AddRange(RestaurantSeed.MenuItems);
        dbContext.MenuItemVariants.AddRange(RestaurantSeed.Variants);
        dbContext.MenuItemAddons.AddRange(RestaurantSeed.Addons);
        dbContext.RestaurantDeals.AddRange(RestaurantSeed.Deals);
        dbContext.RestaurantDealItems.AddRange(RestaurantSeed.DealItems);
        dbContext.RestaurantDealChoiceGroups.AddRange(RestaurantSeed.DealChoiceGroups);

        dbContext.CourierPricingProfiles.AddRange(CourierSeed.Profiles);
        dbContext.CourierDistanceBands.AddRange(CourierSeed.DistanceBands);
        dbContext.CourierWeightBands.AddRange(CourierSeed.WeightBands);
        dbContext.CourierZones.AddRange(CourierSeed.Zones);

        dbContext.KnowledgeBases.AddRange(RagSeed.Bases);
        dbContext.KnowledgeDocuments.AddRange(RagSeed.Documents);
        dbContext.KnowledgeChunks.AddRange(RagSeed.Chunks);

        dbContext.ExternalApiConfigurations.AddRange(ExternalApiSeed.All);
        dbContext.OutboundLeads.AddRange(SalesSeed.Leads);
        dbContext.PlatformUsers.AddRange(AuthSeed.AsEntities());

        await dbContext.SaveChangesAsync();

        var seededRoles = string.Join(", ", AuthSeed.Users.Select(u => u.Role).Distinct());
        logger?.LogInformation("Seeded mock auth users: {Count}. Roles available: {Roles}", AuthSeed.Users.Count, seededRoles);
        logger?.LogInformation("Seed completed");
    }
}
