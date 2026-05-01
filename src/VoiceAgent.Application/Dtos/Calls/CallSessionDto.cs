namespace VoiceAgent.Application.Dtos.Calls;

public class CallSessionResponseDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? CollectedSlotsJson { get; set; }
    public string? FinalResultJson { get; set; }
}

public class CallTurnResponseDto
{
    public Guid Id { get; set; }
    public int TurnNumber { get; set; }
    public string Speaker { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

public class CallEventResponseDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EventDataJson { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class ToolCallLogResponseDto
{
    public Guid Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public DateTime CreatedOn { get; set; }
}
