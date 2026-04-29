namespace VoiceAgent.Application.DTOs.Demo;

public sealed class VoiceTurnRequestDto
{
    public Guid CallSessionId { get; set; }
    public string Transcript { get; set; } = default!;
    public bool IsFinalTranscript { get; set; }
    public bool BargeInDetected { get; set; }
}

public sealed class VoiceTurnResponseDto
{
    public string ReplyText { get; set; } = default!;
    public bool GenerateAudio { get; set; }
    public string? AudioCacheKey { get; set; }
    public bool StopCurrentPlayback { get; set; }
}
