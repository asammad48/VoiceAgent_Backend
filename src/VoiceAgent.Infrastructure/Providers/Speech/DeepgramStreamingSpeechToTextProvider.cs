using VoiceAgent.Application.Interfaces.Providers;

namespace VoiceAgent.Infrastructure.Providers.Speech;

public class DeepgramStreamingSpeechToTextProvider : IStreamingSpeechToTextProvider
{
    private readonly DeepgramClient _client;
    public DeepgramStreamingSpeechToTextProvider(DeepgramClient client) => _client = client;
    public Task<string> TranscribeChunkAsync(byte[] audioChunk, CancellationToken ct = default) => _client.TranscribeAsync(audioChunk, ct);
}
