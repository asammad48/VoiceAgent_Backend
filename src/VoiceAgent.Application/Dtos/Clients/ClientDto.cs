namespace VoiceAgent.Application.Dtos.Clients;
public class CreateClientRequestDto { public Guid TenantId { get; set; } public string Name { get; set; } = string.Empty; public string IndustryType { get; set; } = string.Empty; public string AgentName { get; set; } = string.Empty; }
public class ClientResponseDto { public Guid Id { get; set; } public Guid TenantId { get; set; } public string Name { get; set; } = string.Empty; public string IndustryType { get; set; } = string.Empty; public string AgentName { get; set; } = string.Empty; public bool IsActive { get; set; } }
