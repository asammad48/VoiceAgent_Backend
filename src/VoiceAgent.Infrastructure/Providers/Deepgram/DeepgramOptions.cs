namespace VoiceAgent.Infrastructure.Providers.Deepgram;

public class DeepgramOptions
{
    public const string SectionName = "Deepgram";
    public string BaseUrl { get; set; } = "https://api.deepgram.com";
    public string ApiKey { get; set; } = string.Empty;
}
