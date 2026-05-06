namespace VoiceAgent.Application.Dtos.Clients;

public sealed class UpdateClientRequestDto
{
    public string? Name { get; set; }
    public string? IndustryType { get; set; }
    public string? AgentName { get; set; }
    public bool? IsActive { get; set; }
}
