using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class RestaurantSeed
{
    public static readonly Guid MenuId = Guid.Parse("20000000-0000-0000-0000-000000000401");

    public static readonly IReadOnlyList<RestaurantMenu> Menus =
    [
        new() { Id = MenuId, TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, BranchId = SeedIds.RestaurantBranch, Name = "Belgravia Demo Menu", IsActive = true }
    ];

    public static readonly IReadOnlyList<MenuCategory> Categories =
    [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000411"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, Name = "Burgers", SortOrder = 1 },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000412"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, Name = "Pizza", SortOrder = 2 },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000413"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, Name = "Fries", SortOrder = 3 },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000414"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, Name = "Loaded Fries", SortOrder = 4 },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000415"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, Name = "Drinks", SortOrder = 5 },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000416"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, Name = "Desserts", SortOrder = 6 },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000417"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, Name = "Deals", SortOrder = 7 }
    ];

    public static readonly IReadOnlyList<MenuItem> MenuItems =
    [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000421"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000411"), Name = "Classic Chicken Burger", Description = "Crispy chicken fillet, lettuce, mayo, soft bun", BasePrice = 5.99m, Currency = "GBP", IsAvailable = true, PreparationTimeMinutes = 20, MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000422"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000411"), Name = "Spicy Chicken Burger", Description = "Spicy crispy chicken, jalapenos, chilli mayo", BasePrice = 6.49m, Currency = "GBP", IsAvailable = true, MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000423"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000411"), Name = "Beef Smash Burger", Description = "Double smashed beef patty, cheese, pickles, house sauce", BasePrice = 6.99m, Currency = "GBP", IsAvailable = true, MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000424"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000411"), Name = "Veggie Burger", Description = "Vegetable patty, lettuce, tomato, garlic mayo", BasePrice = 5.49m, Currency = "GBP", IsAvailable = true, MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000431"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000412"), Name = "Margherita Pizza", Description = "Tomato base, mozzarella, basil", BasePrice = 7.99m, Currency = "GBP", IsAvailable = true, MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000432"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000412"), Name = "Pepperoni Pizza", Description = "Tomato base, mozzarella, pepperoni", BasePrice = 9.49m, Currency = "GBP", IsAvailable = true, MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000433"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000412"), Name = "BBQ Chicken Pizza", Description = "BBQ base, chicken, onion, peppers", BasePrice = 9.99m, Currency = "GBP", IsAvailable = true, MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000434"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000412"), Name = "Veggie Supreme Pizza", Description = "Peppers, onion, sweetcorn, mushrooms", BasePrice = 8.99m, Currency = "GBP", IsAvailable = true, MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000441"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000413"), Name = "Regular Fries", Description = "", BasePrice = 2.49m, Currency = "GBP", IsAvailable = true, MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000451"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuId = MenuId, CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000415"), Name = "Coke Can", Description = "", BasePrice = 1.49m, Currency = "GBP", IsAvailable = true, MetadataJson = "{}" }
    ];

    public static readonly IReadOnlyList<MenuItemVariant> Variants =
    [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000461"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuItemId = Guid.Parse("20000000-0000-0000-0000-000000000431"), Name = "Small", PriceDelta = 0, IsAvailable = true },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000462"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuItemId = Guid.Parse("20000000-0000-0000-0000-000000000431"), Name = "Medium", PriceDelta = 2, IsAvailable = true },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000463"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, MenuItemId = Guid.Parse("20000000-0000-0000-0000-000000000431"), Name = "Large", PriceDelta = 4, IsAvailable = true }
    ];

    public static readonly IReadOnlyList<MenuItemAddon> Addons =
    [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000471"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, Name = "Cheese Slice", Price = 0.70m, IsAvailable = true },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000472"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, Name = "Extra Patty", Price = 2.00m, IsAvailable = true }
    ];

    public static readonly IReadOnlyList<RestaurantDeal> Deals =
    [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000481"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, BranchId = SeedIds.RestaurantBranch, Name = "Burger Combo", Description = "Any chicken burger, regular fries, and drink", DealPrice = 8.99m, Currency = "GBP", IsAvailable = true, IsActive = true, AvailabilityScheduleJson = "{}", MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000482"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, BranchId = SeedIds.RestaurantBranch, Name = "Family Pizza Deal", Description = "Two large pizzas, two fries, and four drinks", DealPrice = 24.99m, Currency = "GBP", IsAvailable = true, IsActive = true, AvailabilityScheduleJson = "{}", MetadataJson = "{}" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000483"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, BranchId = SeedIds.RestaurantBranch, Name = "Student Meal Deal", Description = "Burger, fries, and drink for students", DealPrice = 7.49m, Currency = "GBP", IsAvailable = true, IsActive = true, AvailabilityScheduleJson = "{}", MetadataJson = "{}" }
    ];

    public static readonly IReadOnlyList<RestaurantDealItem> DealItems =
    [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000491"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, DealId = Guid.Parse("20000000-0000-0000-0000-000000000481"), MenuItemId = Guid.Parse("20000000-0000-0000-0000-000000000421"), Quantity = 1, IsRequired = true }
    ];

    public static readonly IReadOnlyList<RestaurantDealChoiceGroup> DealChoiceGroups =
    [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000492"), TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, DealId = Guid.Parse("20000000-0000-0000-0000-000000000481"), Name = "Choose your drink", MinSelections = 1, MaxSelections = 1, SortOrder = 1, OptionsJson = "[\"Coke Can\",\"Diet Coke Can\",\"Sprite Can\"]" }
    ];
}
