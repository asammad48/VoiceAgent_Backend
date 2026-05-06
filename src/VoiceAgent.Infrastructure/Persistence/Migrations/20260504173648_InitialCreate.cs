using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    DataJson = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    DeliveryRadiusKm = table.Column<decimal>(type: "numeric", nullable: true),
                    DeliveryFeeRulesJson = table.Column<string>(type: "text", nullable: true),
                    BusinessHoursJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CallCostLogs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LlmInputTokens = table.Column<int>(type: "integer", nullable: false),
                    LlmOutputTokens = table.Column<int>(type: "integer", nullable: false),
                    TtsCharacters = table.Column<int>(type: "integer", nullable: false),
                    SttAudioSeconds = table.Column<int>(type: "integer", nullable: false),
                    CallDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallCostLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CallEvents",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    EventDataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CallRecordings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageProvider = table.Column<string>(type: "text", nullable: false),
                    Bucket = table.Column<string>(type: "text", nullable: true),
                    ObjectKey = table.Column<string>(type: "text", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallRecordings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CallSessions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    ExternalCallId = table.Column<string>(type: "text", nullable: true),
                    CallerPhone = table.Column<string>(type: "text", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentState = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    CollectedSlotsJson = table.Column<string>(type: "jsonb", nullable: true),
                    FinalResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    SummaryJson = table.Column<string>(type: "jsonb", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    HandoffAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    HandoffRequested = table.Column<bool>(type: "boolean", nullable: false),
                    HandoffCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    HandoffReason = table.Column<string>(type: "text", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CallTurns",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurnNumber = table.Column<int>(type: "integer", nullable: false),
                    Speaker = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Intent = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    StateBefore = table.Column<string>(type: "text", nullable: true),
                    StateAfter = table.Column<string>(type: "text", nullable: true),
                    ToolName = table.Column<string>(type: "text", nullable: true),
                    ToolResultJson = table.Column<string>(type: "text", nullable: true),
                    LatencyMs = table.Column<int>(type: "integer", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallTurns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampaignConfigurations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredSlotsJson = table.Column<string>(type: "jsonb", nullable: false),
                    OptionalSlotsJson = table.Column<string>(type: "jsonb", nullable: true),
                    AllowedToolsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ValidationRulesJson = table.Column<string>(type: "text", nullable: true),
                    FallbackRulesJson = table.Column<string>(type: "text", nullable: true),
                    ConfirmationRulesJson = table.Column<string>(type: "text", nullable: true),
                    LlmSettingsJson = table.Column<string>(type: "text", nullable: true),
                    VoiceSettingsJson = table.Column<string>(type: "text", nullable: true),
                    RagSettingsJson = table.Column<string>(type: "text", nullable: true),
                    HumanTransferJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CampaignType = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    IsDemoEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IndustryType = table.Column<string>(type: "text", nullable: false),
                    AgentName = table.Column<string>(type: "text", nullable: false),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    SettingsJson = table.Column<string>(type: "text", nullable: true),
                    CallRecordingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactUsMessages",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    ResolutionStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactUsMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourierDistanceBands",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierPricingProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromKm = table.Column<decimal>(type: "numeric", nullable: false),
                    ToKm = table.Column<decimal>(type: "numeric", nullable: false),
                    Fee = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierDistanceBands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourierOrders",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierQuoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    FinalResultJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourierPricingProfiles",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    BaseFee = table.Column<decimal>(type: "numeric", nullable: false),
                    PricePerKm = table.Column<decimal>(type: "numeric", nullable: false),
                    PricePerKg = table.Column<decimal>(type: "numeric", nullable: false),
                    MinimumFee = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxDistanceKm = table.Column<decimal>(type: "numeric", nullable: false),
                    SettingsJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierPricingProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourierQuotes",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickupAddressJson = table.Column<string>(type: "text", nullable: false),
                    DropoffAddressJson = table.Column<string>(type: "text", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "numeric", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric", nullable: false),
                    PackageType = table.Column<string>(type: "text", nullable: false),
                    Urgency = table.Column<string>(type: "text", nullable: false),
                    EstimatedDeliveryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BaseFee = table.Column<decimal>(type: "numeric", nullable: false),
                    DistanceFee = table.Column<decimal>(type: "numeric", nullable: false),
                    WeightFee = table.Column<decimal>(type: "numeric", nullable: false),
                    UrgencyFee = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierQuotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourierWeightBands",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierPricingProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromKg = table.Column<decimal>(type: "numeric", nullable: false),
                    ToKg = table.Column<decimal>(type: "numeric", nullable: false),
                    Fee = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierWeightBands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourierZones",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierPricingProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ZoneJson = table.Column<string>(type: "text", nullable: false),
                    ExtraFee = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalApiConfigurations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    AuthType = table.Column<string>(type: "text", nullable: false),
                    HeadersJson = table.Column<string>(type: "jsonb", nullable: false),
                    EndpointsJson = table.Column<string>(type: "text", nullable: false),
                    SecretReferenceJson = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalApiConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalSystemLogs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalApiConfigurationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestJson = table.Column<string>(type: "text", nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalSystemLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeBases",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeChunks",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeBaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkText = table.Column<string>(type: "text", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "jsonb", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeChunks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeBaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuCategories",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemAddons",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemAddons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    PreparationTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemVariants",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PriceDelta = table.Column<decimal>(type: "numeric", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemVariants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboundAttempts",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboundLeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboundCampaignRuns",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundCampaignRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboundLeads",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    DataJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    OptedOut = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundLeads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformUsers",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptVersions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantDealAddons",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemAddonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraPrice = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantDealAddons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantDealChoiceGroups",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MinSelections = table.Column<int>(type: "integer", nullable: false),
                    MaxSelections = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OptionsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantDealChoiceGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantDealItems",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantDealItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantDeals",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DealPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AvailabilityScheduleJson = table.Column<string>(type: "jsonb", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantDeals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantMenus",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantMenus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantOrders",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    FulfillmentType = table.Column<string>(type: "text", nullable: false),
                    AddressJson = table.Column<string>(type: "text", nullable: false),
                    ItemsJson = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExternalReference = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    DefaultTimezone = table.Column<string>(type: "text", nullable: false),
                    DefaultCurrency = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolCallLogs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolName = table.Column<string>(type: "text", nullable: false),
                    RequestJson = table.Column<string>(type: "text", nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolCallLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolDefinitions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUsers_TenantId_Email",
                schema: "public",
                table: "PlatformUsers",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Branches",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CallCostLogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CallEvents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CallRecordings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CallSessions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CallTurns",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CampaignConfigurations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Campaigns",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Clients",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ContactUsMessages",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CourierDistanceBands",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CourierOrders",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CourierPricingProfiles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CourierQuotes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CourierWeightBands",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CourierZones",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ExternalApiConfigurations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ExternalSystemLogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "KnowledgeBases",
                schema: "public");

            migrationBuilder.DropTable(
                name: "KnowledgeChunks",
                schema: "public");

            migrationBuilder.DropTable(
                name: "KnowledgeDocuments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MenuCategories",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MenuItemAddons",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MenuItems",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MenuItemVariants",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OutboundAttempts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OutboundCampaignRuns",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OutboundLeads",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PlatformUsers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PromptVersions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RestaurantDealAddons",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RestaurantDealChoiceGroups",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RestaurantDealItems",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RestaurantDeals",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RestaurantMenus",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RestaurantOrders",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ToolCallLogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ToolDefinitions",
                schema: "public");
        }
    }
}
