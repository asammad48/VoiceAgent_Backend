namespace VoiceAgent.Application.Dtos.Deals;
public class CreateDealRequestDto { public Guid TenantId { get; set; } public Guid ClientId { get; set; } public Guid? BranchId { get; set; } public string Name { get; set; } = string.Empty; public decimal DealPrice { get; set; } public string Currency { get; set; } = string.Empty; }
public class DealResponseDto { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; public decimal DealPrice { get; set; } public string Currency { get; set; } = string.Empty; public bool IsAvailable { get; set; } }
