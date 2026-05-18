namespace VoiceAgent.Application.Services.Voice;

/// <summary>
/// Prevents excessively long text from being sent to ElevenLabs.
/// Truncates at the last sentence boundary before the character limit,
/// preserving natural speech endings. Pure static — no dependencies.
/// </summary>
public static class TtsResponseGuard
{
    // ElevenLabs charges per character. 350 chars covers all normal conversational turns
    // (questions, quotes, confirmations) while blocking runaway LLM / RAG responses.
    public const int MaxChars = 350;

    public static string Truncate(string text)
    {
        if (text.Length <= MaxChars) return text;

        // Walk back from the limit looking for a sentence-end punctuation mark.
        // Only search down to half the limit so we don't produce a severely short reply.
        for (var i = MaxChars - 1; i >= MaxChars / 2; i--)
        {
            if (text[i] is '.' or '!' or '?')
                return text[..(i + 1)];
        }

        // No sentence boundary found — fall back to last word boundary.
        var wordBreak = text.LastIndexOf(' ', MaxChars);
        return wordBreak > 0
            ? text[..wordBreak].TrimEnd() + "."
            : text[..MaxChars].TrimEnd() + ".";
    }
}
