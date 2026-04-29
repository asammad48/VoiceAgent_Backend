namespace VoiceAgent.Infrastructure.Providers.Deepgram;

public class DeepgramClient(HttpClient httpClient)
{
    public HttpClient HttpClient { get; } = httpClient;
}
