using VoiceAgent.Application.Interfaces.Voice;

namespace VoiceAgent.Infrastructure.Providers.Voice;

/// <summary>
/// No-op TTS normalizer used when UseMockProviders = true.
/// Returns text unchanged so the rest of the pipeline still works without API keys.
/// </summary>
public sealed class PassThroughTtsNormalizationService : ITtsNormalizationService
{
    public Task<string> NormalizeForTtsAsync(string text, CancellationToken ct = default)
        => Task.FromResult(text);
}
