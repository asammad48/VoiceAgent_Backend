using System.Text;
using VoiceAgent.Application.Interfaces.Providers;

namespace VoiceAgent.Infrastructure.Providers.Voice;

public class ElevenLabsStreamingTextToSpeechProvider : IStreamingTextToSpeechProvider
{
    private readonly ElevenLabsClient _client;
    public ElevenLabsStreamingTextToSpeechProvider(ElevenLabsClient client) => _client = client;

    public async Task<byte[]> SynthesizeChunkAsync(string text, CancellationToken ct = default)
    {
        var audio = await _client.SynthesizeAsync(text, ct);
        return audio.Length == 0 ? Encoding.UTF8.GetBytes($"[tts-fallback]{text}") : audio;
    }
}
