using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignConfiguration> CampaignConfigurations => Set<CampaignConfiguration>();
    public DbSet<CallSession> CallSessions => Set<CallSession>();
    public DbSet<CallTurn> CallTurns => Set<CallTurn>();
    public DbSet<CallEvent> CallEvents => Set<CallEvent>();
    public DbSet<RestaurantMenu> RestaurantMenus => Set<RestaurantMenu>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<RestaurantDeal> RestaurantDeals => Set<RestaurantDeal>();
    public DbSet<CourierPricingProfile> CourierPricingProfiles => Set<CourierPricingProfile>();
    public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<ExternalApiConfiguration> ExternalApiConfigurations => Set<ExternalApiConfiguration>();
    public DbSet<ToolCallLog> ToolCallLogs => Set<ToolCallLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
    }
}
