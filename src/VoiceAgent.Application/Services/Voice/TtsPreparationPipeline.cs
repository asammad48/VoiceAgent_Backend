using VoiceAgent.Application.Interfaces.Voice;

namespace VoiceAgent.Application.Services.Voice;

/// <summary>
/// Orchestrates all TTS preparation steps before ElevenLabs synthesis:
///   1. Sanitize  — strip markdown, debug labels, URLs, emojis, JSON artefacts
///   2. Guard     — truncate if over the character limit (before LLM call to save tokens)
///   3. Normalize — call LLM to convert to natural spoken English
///
/// Registered as scoped (matches the lifetime of its ITtsNormalizationService dependency).
/// </summary>
public sealed class TtsPreparationPipeline(ITtsNormalizationService normalizationService)
{
    public async Task<string> PrepareAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        text = TtsInputSanitizer.Sanitize(text);
        text = TtsResponseGuard.Truncate(text);          // trim before LLM call to save tokens
        text = await normalizationService.NormalizeForTtsAsync(text, ct);

        return text.Trim();
    }
}
