namespace VoiceAgent.Application.DTOs.Voice;

public sealed class VoiceStartResponseDto
{
    public Guid CallSessionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? Status { get; set; }
}
