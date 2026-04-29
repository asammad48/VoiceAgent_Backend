namespace VoiceAgent.Application.DTOs.Demo;

public sealed class ConversationMemoryDto
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string CurrentState { get; set; } = "CollectingSlots";
    public string? CurrentIntent { get; set; }
    public Dictionary<string, object?> CollectedSlots { get; set; } = new();
    public List<string> MissingSlots { get; set; } = new();
    public Dictionary<string, object?> CurrentCart { get; set; } = new();
    public Dictionary<string, object?> CurrentQuote { get; set; } = new();
    public Dictionary<string, object?> LastToolResult { get; set; } = new();
    public int FailureCount { get; set; }
    public bool HandoffRequired { get; set; }
}
