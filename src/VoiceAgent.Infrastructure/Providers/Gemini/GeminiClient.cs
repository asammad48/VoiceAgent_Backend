namespace VoiceAgent.Infrastructure.Providers.Gemini;

public class GeminiClient(HttpClient httpClient)
{
    public HttpClient HttpClient { get; } = httpClient;
}
