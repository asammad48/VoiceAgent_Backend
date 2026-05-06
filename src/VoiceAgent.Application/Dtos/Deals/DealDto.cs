namespace VoiceAgent.Application.Dtos.Deals;

public class CreateDealRequestDto { public Guid TenantId { get; set; } public Guid ClientId { get; set; } public Guid? BranchId { get; set; } public string Name { get; set; } = string.Empty; public decimal DealPrice { get; set; } public string Currency { get; set; } = string.Empty; }
public class UpdateDealRequestDto { public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public decimal DealPrice { get; set; } public string Currency { get; set; } = string.Empty; public bool IsAvailable { get; set; } }
public class DealResponseDto { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; public decimal DealPrice { get; set; } public string Currency { get; set; } = string.Empty; public bool IsAvailable { get; set; } }

public class UpsertDealItemRequestDto { public Guid TenantId { get; set; } public Guid ClientId { get; set; } public Guid MenuItemId { get; set; } public Guid? MenuItemVariantId { get; set; } public int Quantity { get; set; } public bool IsRequired { get; set; } }
public class UpsertDealAddonRequestDto { public Guid TenantId { get; set; } public Guid ClientId { get; set; } public Guid MenuItemAddonId { get; set; } public int Quantity { get; set; } public bool IsIncluded { get; set; } public decimal ExtraPrice { get; set; } }
public class UpsertDealChoiceGroupRequestDto { public Guid TenantId { get; set; } public Guid ClientId { get; set; } public string Name { get; set; } = string.Empty; public int MinSelections { get; set; } public int MaxSelections { get; set; } public int SortOrder { get; set; } public string OptionsJson { get; set; } = "[]"; }
