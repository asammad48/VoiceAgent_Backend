using VoiceAgent.Common.Entities;

namespace VoiceAgent.Domain.Entities;

public class RestaurantMenu : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MenuCategory : AuditableEntity
{
    public Guid MenuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class MenuItem : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid MenuId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int? PreparationTimeMinutes { get; set; }
    public string? MetadataJson { get; set; }
}

public class MenuItemVariant : AuditableEntity
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
    public bool IsAvailable { get; set; }
}

public class MenuItemAddon : AuditableEntity
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}

public class RestaurantDeal : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DealPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? AvailabilityScheduleJson { get; set; }
    public string? MetadataJson { get; set; }
}

public class RestaurantDealItem : AuditableEntity
{
    public Guid DealId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? MenuItemVariantId { get; set; }
    public int Quantity { get; set; }
    public bool IsRequired { get; set; }
}

public class RestaurantDealAddon : AuditableEntity
{
    public Guid DealId { get; set; }
    public Guid MenuItemAddonId { get; set; }
    public int Quantity { get; set; }
    public bool IsIncluded { get; set; }
    public decimal ExtraPrice { get; set; }
}

public class RestaurantDealChoiceGroup : AuditableEntity
{
    public Guid DealId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public int SortOrder { get; set; }
}

public class RestaurantOrder : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string? CustomerName { get; set; }
    public string? Phone { get; set; }
    public string? FulfillmentType { get; set; }
    public string? AddressJson { get; set; }
    public string? ItemsJson { get; set; }
    public string? DealsJson { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
}
