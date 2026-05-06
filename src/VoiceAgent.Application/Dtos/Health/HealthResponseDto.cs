namespace VoiceAgent.Application.Dtos.Health;

public class HealthResponseDto
{
    public string Status { get; set; } = "Healthy";
    public string Service { get; set; } = "VoiceAgent.Api";
    public string Environment { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}
