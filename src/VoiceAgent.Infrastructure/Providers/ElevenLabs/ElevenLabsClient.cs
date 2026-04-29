namespace VoiceAgent.Infrastructure.Providers.ElevenLabs;

public class ElevenLabsClient(HttpClient httpClient)
{
    public HttpClient HttpClient { get; } = httpClient;
}
