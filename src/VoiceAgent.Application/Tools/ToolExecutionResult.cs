namespace VoiceAgent.Application.Tools;

public sealed class ToolExecutionResult
{
    public bool Success { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string? UserMessage { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ShouldRetry { get; set; }
    public bool RequiresHumanHandoff { get; set; }
}
