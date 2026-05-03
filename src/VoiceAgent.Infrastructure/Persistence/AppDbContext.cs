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
    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();
    public DbSet<CallSession> CallSessions => Set<CallSession>();
    public DbSet<CallTurn> CallTurns => Set<CallTurn>();
    public DbSet<CallEvent> CallEvents => Set<CallEvent>();
    public DbSet<CallCostLog> CallCostLogs => Set<CallCostLog>();
    public DbSet<CallRecording> CallRecordings => Set<CallRecording>();
    public DbSet<RestaurantMenu> RestaurantMenus => Set<RestaurantMenu>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemAddon> MenuItemAddons => Set<MenuItemAddon>();
    public DbSet<MenuItemVariant> MenuItemVariants => Set<MenuItemVariant>();
    public DbSet<RestaurantDeal> RestaurantDeals => Set<RestaurantDeal>();
    public DbSet<RestaurantDealItem> RestaurantDealItems => Set<RestaurantDealItem>();
    public DbSet<RestaurantDealAddon> RestaurantDealAddons => Set<RestaurantDealAddon>();
    public DbSet<RestaurantDealChoiceGroup> RestaurantDealChoiceGroups => Set<RestaurantDealChoiceGroup>();
    public DbSet<RestaurantOrder> RestaurantOrders => Set<RestaurantOrder>();
    public DbSet<CourierPricingProfile> CourierPricingProfiles => Set<CourierPricingProfile>();
    public DbSet<CourierDistanceBand> CourierDistanceBands => Set<CourierDistanceBand>();
    public DbSet<CourierWeightBand> CourierWeightBands => Set<CourierWeightBand>();
    public DbSet<CourierZone> CourierZones => Set<CourierZone>();
    public DbSet<CourierQuote> CourierQuotes => Set<CourierQuote>();
    public DbSet<CourierOrder> CourierOrders => Set<CourierOrder>();
    public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<ExternalApiConfiguration> ExternalApiConfigurations => Set<ExternalApiConfiguration>();
    public DbSet<ToolDefinition> ToolDefinitions => Set<ToolDefinition>();
    public DbSet<ToolCallLog> ToolCallLogs => Set<ToolCallLog>();
    public DbSet<ExternalSystemLog> ExternalSystemLogs => Set<ExternalSystemLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<ContactUs> ContactUsMessages => Set<ContactUs>();
    public DbSet<OutboundCampaignRun> OutboundCampaignRuns => Set<OutboundCampaignRun>();
    public DbSet<OutboundLead> OutboundLeads => Set<OutboundLead>();
    public DbSet<OutboundAttempt> OutboundAttempts => Set<OutboundAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.Entity<CallSession>().Property(x => x.CollectedSlotsJson).HasColumnType("jsonb");
        modelBuilder.Entity<CallSession>().Property(x => x.FinalResultJson).HasColumnType("jsonb");
        modelBuilder.Entity<CallSession>().Property(x => x.SummaryJson).HasColumnType("jsonb");
        modelBuilder.Entity<CampaignConfiguration>().Property(x => x.RequiredSlotsJson).HasColumnType("jsonb");
        modelBuilder.Entity<CampaignConfiguration>().Property(x => x.OptionalSlotsJson).HasColumnType("jsonb");
        modelBuilder.Entity<CampaignConfiguration>().Property(x => x.AllowedToolsJson).HasColumnType("jsonb");
                modelBuilder.Entity<MenuItem>().Property(x => x.MetadataJson).HasColumnType("jsonb");
        modelBuilder.Entity<RestaurantDeal>().Property(x => x.AvailabilityScheduleJson).HasColumnType("jsonb");
        modelBuilder.Entity<RestaurantDeal>().Property(x => x.MetadataJson).HasColumnType("jsonb");
        modelBuilder.Entity<KnowledgeChunk>().Property(x => x.EmbeddingJson).HasColumnType("jsonb");
        modelBuilder.Entity<KnowledgeChunk>().Property(x => x.MetadataJson).HasColumnType("jsonb");
        modelBuilder.Entity<ExternalApiConfiguration>().Property(x => x.HeadersJson).HasColumnType("jsonb");

        modelBuilder.Entity<PlatformUser>()
            .HasIndex(x => new { x.TenantId, x.Email })
            .IsUnique();
    }
}
