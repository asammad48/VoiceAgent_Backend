namespace VoiceAgent.Application.DTOs.Demo;

public sealed class StartDemoRequestDto
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid? BranchId { get; set; }
    public string Channel { get; set; } = "WebText";
}

public sealed class StartDemoResponseDto
{
    public Guid CallSessionId { get; set; }
    public string AgentName { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string CurrentState { get; set; } = default!;
}
