namespace VoiceAgent.Application.Dtos.Branches;
public class CreateBranchRequestDto { public Guid TenantId { get; set; } public Guid ClientId { get; set; } public string Name { get; set; } = string.Empty; public string? Address { get; set; } }
public class BranchResponseDto { public Guid Id { get; set; } public Guid TenantId { get; set; } public Guid ClientId { get; set; } public string Name { get; set; } = string.Empty; public string? Address { get; set; } public bool IsActive { get; set; } }
