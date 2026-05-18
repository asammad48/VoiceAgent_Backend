using Microsoft.Extensions.Caching.Memory;

namespace VoiceAgent.Infrastructure.Providers.Voice;

/// <summary>
/// In-process audio cache for ElevenLabs synthesis results.
/// Short, static phrases (greetings, confirmations, re-prompts) are cached for 24 hours so
/// repeated turns in the same day skip the ElevenLabs round-trip entirely.
/// Singleton — shared across all sessions, all WebSocket connections.
/// </summary>
public sealed class TtsAudioCache(IMemoryCache cache)
{
    // Only cache phrases below this length — long dynamic responses (addresses, quotes)
    // change too frequently to benefit from caching.
    private const int MaxCacheableChars = 180;

    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    public bool TryGet(string text, out byte[] audio) =>
        cache.TryGetValue(CacheKey(text), out audio!);

    public void Set(string text, byte[] audio)
    {
        if (text.Length <= MaxCacheableChars && audio.Length > 0)
            cache.Set(CacheKey(text), audio, Ttl);
    }

    // Use the raw text as the key so identical strings always hit the same entry.
    private static string CacheKey(string text) => $"tts:{text}";
}
