using Microsoft.Extensions.Options;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Infrastructure.Providers.Llm;

namespace VoiceAgent.Infrastructure.Providers;

public sealed class GeminiSlotExtractionService(
    GeminiClient geminiClient,
    IOptions<GeminiOptions> options) : ISlotExtractionService
{
    public async Task<string?> ExtractAsync(string slotId, string question, string userMessage, string? slotType, CancellationToken ct = default)
    {
        // When mocking, behave as null so the regex path is the only path
        if (options.Value.UseMockProviders) return null;

        var typeConstraint = slotType switch
        {
            "date" => """

                Type constraint — DATE: The answer must reference a specific date, day of week, or relative date (e.g. "tomorrow", "next Monday", "March 15th"). A lone preposition like "On" or "At" without a date is NOT valid — return null in that case.
                """,
            "datetime" => """

                Type constraint — DATETIME: The answer must contain a date and/or time reference (e.g. "Monday at 3pm", "tomorrow morning", "tonight at 9"). A lone preposition is NOT valid — return null.
                """,
            "number" => """

                Type constraint — NUMBER: Return only the numeric value as a digit (e.g. "2" not "two people").
                """,
            "phone" => """

                Type constraint — PHONE NUMBER: Return the digits with any standard separators (spaces, dashes, plus sign). No other text.
                """,
            "yesno" => """

                Type constraint — YES/NO: Return exactly "Yes" or "No".
                """,
            _ => string.Empty
        };

        var prompt = $"""
            You are helping a voice AI agent extract a specific slot value from a user's spoken reply.

            Slot name   : {slotId}
            Question    : {question}
            User replied: {userMessage}
            {typeConstraint}
            Rules:
            - Return ONLY the extracted value as plain text — no quotes, no explanation.
            - If valid values are listed above, return one of those exactly (case-sensitive).
            - If the user clearly said no / not interested / not applicable, return: None
            - If the user gave no clear answer at all, return: null
            """;

        var raw = await geminiClient.GenerateAsync(prompt, ct);
        var trimmed = raw.Trim().Trim('"', '\'');

        if (string.IsNullOrWhiteSpace(trimmed)
            || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("[mock-gemini]", StringComparison.OrdinalIgnoreCase))
            return null;

        return trimmed;
    }
}
