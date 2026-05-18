namespace VoiceAgent.Application.Interfaces.Voice;

/// <summary>
/// Converts machine-readable assistant text into human-speakable form before TTS synthesis.
/// The implementation calls an LLM; a pass-through mock is used when UseMockProviders = true.
/// </summary>
public interface ITtsNormalizationService
{
    Task<string> NormalizeForTtsAsync(string text, CancellationToken ct = default);
}
