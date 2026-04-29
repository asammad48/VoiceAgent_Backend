namespace VoiceAgent.Infrastructure.Providers.ElevenLabs;

public class ElevenLabsOptions
{
    public const string SectionName = "ElevenLabs";
    public string BaseUrl { get; set; } = "https://api.elevenlabs.io";
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultVoiceId { get; set; } = string.Empty;
}
