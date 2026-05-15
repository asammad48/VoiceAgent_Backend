using Microsoft.Extensions.Options;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Infrastructure.Providers.Llm;

namespace VoiceAgent.Infrastructure.Providers;

public sealed class GeminiAnswerFinalizationService(
    GeminiClient geminiClient,
    IOptions<GeminiOptions> options) : IAnswerFinalizationService
{
    public async Task<AnswerFinalizationResult> FinalizeAnswersAsync(
        IReadOnlyList<SlotAnswer> answers,
        CancellationToken ct = default)
    {
        if (options.Value.UseMockProviders)
            return new AnswerFinalizationResult(true, []);

        var qaPairs = string.Join("\n", answers.Select(a =>
            $"  slotId={a.SlotId} type={a.SlotType ?? "text"}: Q=\"{a.Question}\" A=\"{a.Answer}\""));

        var prompt = $"""
            You are a quality checker for a voice AI agent. Review these collected question-answer pairs and identify any where the answer is ambiguous, clearly wrong, or missing required information.

            Collected answers:
            {qaPairs}

            Validation rules by slotType:
            - date     : Must reference a specific date, day of week, or relative date ("tomorrow", "next Monday", "March 15th"). A lone preposition like "On" or "At" is NOT valid.
            - datetime : Must contain a date AND/OR time reference ("Monday at 3pm", "tomorrow morning", "tonight at 9"). A lone preposition is NOT valid.
            - number   : Must be a recognizable number (digit or word like "two").
            - phone    : Must look like a phone number (digits, spaces, dashes, plus sign).
            - yesno    : Must be clearly yes or no.
            - text     : Must be a meaningful non-empty response (not just filler or a single preposition).

            If ALL answers look valid and unambiguous, reply with exactly: ALL_CLEAR
            If any answers are ambiguous or invalid, reply with their slotIds comma-separated, e.g.: pickupDateTime,passengerCount

            Reply with ONLY "ALL_CLEAR" or a comma-separated list of slotIds. No explanation, no extra text.
            """;

        var raw = await geminiClient.GenerateAsync(prompt, ct);
        var trimmed = raw.Trim();

        if (string.IsNullOrWhiteSpace(trimmed)
            || trimmed.Equals("ALL_CLEAR", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("[mock-gemini]", StringComparison.OrdinalIgnoreCase))
            return new AnswerFinalizationResult(true, []);

        var knownSlotIds = new HashSet<string>(answers.Select(a => a.SlotId), StringComparer.OrdinalIgnoreCase);
        var ambiguous = trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => knownSlotIds.Contains(s))
            .ToList();

        return ambiguous.Count > 0
            ? new AnswerFinalizationResult(false, ambiguous)
            : new AnswerFinalizationResult(true, []);
    }
}
