namespace VoiceAgent.Application.Dtos.Demo;
public class SendDemoMessageRequestDto { public Guid CallSessionId { get; set; } public string Message { get; set; } = string.Empty; }
public class SendDemoMessageResponseDto { public string Reply { get; set; } = string.Empty; public string CurrentState { get; set; } = string.Empty; public List<string> MissingSlots { get; set; } = new(); public object? CurrentCart { get; set; } public object? CurrentQuote { get; set; } public object? FinalResult { get; set; } }

public class EndDemoConversationRequestDto { public Guid CallSessionId { get; set; } }

public class EndDemoConversationResponseDto { public Guid CallSessionId { get; set; } public string Status { get; set; } = string.Empty; }
