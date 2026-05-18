using System.Text;
using VoiceAgent.Application.Interfaces.Providers;

namespace VoiceAgent.Infrastructure.Providers.Voice;

public class ElevenLabsStreamingTextToSpeechProvider(ElevenLabsClient client, TtsAudioCache cache)
    : IStreamingTextToSpeechProvider
{
    public async Task<byte[]> SynthesizeChunkAsync(string text, CancellationToken ct = default)
    {
        if (cache.TryGet(text, out var cached))
            return cached;

        var audio = await client.SynthesizeAsync(text, ct);
        if (audio.Length == 0)
            return Encoding.UTF8.GetBytes($"[tts-fallback]{text}");

        cache.Set(text, audio);
        return audio;
    }
}
