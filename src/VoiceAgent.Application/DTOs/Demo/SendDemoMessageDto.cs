namespace VoiceAgent.Application.DTOs.Demo;

public sealed class SendDemoMessageRequestDto
{
    public Guid CallSessionId { get; set; }
    public string Message { get; set; } = default!;
}

public sealed class SendDemoMessageResponseDto
{
    public string Reply { get; set; } = default!;
    public string CurrentState { get; set; } = default!;
    public List<string> MissingSlots { get; set; } = new();
    public object? CurrentCart { get; set; }
    public object? CurrentQuote { get; set; }
    public object? FinalResult { get; set; }
}
