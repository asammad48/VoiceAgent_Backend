using Microsoft.EntityFrameworkCore;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Client> Clients { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<CampaignConfiguration> CampaignConfigurations { get; }
    DbSet<CallSession> CallSessions { get; }
    DbSet<CallTurn> CallTurns { get; }
    DbSet<CallEvent> CallEvents { get; }
    DbSet<CallCostLog> CallCostLogs { get; }
    DbSet<RestaurantMenu> RestaurantMenus { get; }
    DbSet<MenuCategory> MenuCategories { get; }
    DbSet<MenuItem> MenuItems { get; }
    DbSet<MenuItemAddon> MenuItemAddons { get; }
    DbSet<RestaurantOrder> RestaurantOrders { get; }
    DbSet<CourierQuote> CourierQuotes { get; }
    DbSet<CourierOrder> CourierOrders { get; }
    DbSet<RestaurantDeal> RestaurantDeals { get; }
    DbSet<CourierPricingProfile> CourierPricingProfiles { get; }
    DbSet<KnowledgeBase> KnowledgeBases { get; }
    DbSet<KnowledgeDocument> KnowledgeDocuments { get; }
    DbSet<KnowledgeChunk> KnowledgeChunks { get; }
    DbSet<ExternalApiConfiguration> ExternalApiConfigurations { get; }
    DbSet<ToolCallLog> ToolCallLogs { get; }
    DbSet<PlatformUser> PlatformUsers { get; }
    DbSet<ContactUs> ContactUsMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
