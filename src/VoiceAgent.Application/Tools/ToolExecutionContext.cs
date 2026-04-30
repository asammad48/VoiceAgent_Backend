namespace VoiceAgent.Application.Tools;

public sealed class ToolExecutionContext
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? UserMessage { get; set; }
    public Dictionary<string, object?> Slots { get; set; } = new();
    public Dictionary<string, object?> Memory { get; set; } = new();
}
