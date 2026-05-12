using Microsoft.Extensions.Options;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Infrastructure.Providers.Llm;

namespace VoiceAgent.Infrastructure.Providers;

public sealed class GeminiSlotExtractionService(
    GeminiClient geminiClient,
    IOptions<GeminiOptions> options) : ISlotExtractionService
{
    public async Task<string?> ExtractAsync(string slotId, string question, string userMessage, CancellationToken ct = default)
    {
        // When mocking, behave as null so the regex path is the only path
        if (options.Value.UseMockProviders) return null;

        var validHint = string.Empty; // populated per slot if known
        var prompt = $"""
            You are helping a voice AI agent extract a specific slot value from a user's spoken reply.

            Slot name   : {slotId}
            Question    : {question}
            User replied: {userMessage}
            {validHint}
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
