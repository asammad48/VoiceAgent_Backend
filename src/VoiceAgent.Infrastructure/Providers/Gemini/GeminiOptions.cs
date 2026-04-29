namespace VoiceAgent.Infrastructure.Providers.Gemini;

public class GeminiOptions
{
    public const string SectionName = "Gemini";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "gemini-1.5-flash";
}
