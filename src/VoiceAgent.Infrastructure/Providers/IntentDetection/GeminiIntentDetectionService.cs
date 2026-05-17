using Microsoft.Extensions.Options;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Infrastructure.Providers.Llm;

namespace VoiceAgent.Infrastructure.Providers.IntentDetection;

public class GeminiIntentDetectionService(
    GeminiClient geminiClient,
    IOptions<GeminiOptions> options) : IIntentDetectionService
{
    public async Task<IntentMatch?> DetectAsync(string userMessage, IReadOnlyList<IntentTrigger> intents, CancellationToken ct = default)
    {
        // Try keyword match first — only call LLM when ambiguous
        var keyword = KeywordIntentDetectionService.TryKeywordMatch(userMessage, intents);
        if (keyword is not null) return keyword;

        if (options.Value.UseMockProviders)
        {
            var fallback = intents.FirstOrDefault(i => i.Type != "transfer");
            return fallback is null ? null : new IntentMatch(fallback.Id, 0.5f);
        }

        var intentList = string.Join("\n", intents.Select(i => $"- {i.Id}: {i.Name}"));
        var prompt = $"""
            A caller said: "{userMessage}"

            Which of the following intents best matches their request?
            {intentList}

            Reply with ONLY the intent id (e.g. "book_cab") or "UNKNOWN" if none match clearly.
            """;

        var response = await geminiClient.GenerateAsync(prompt, ct);
        var trimmed = response?.Trim().Trim('"').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "unknown") return null;

        var matched = intents.FirstOrDefault(i => string.Equals(i.Id, trimmed, StringComparison.OrdinalIgnoreCase));
        return matched is null ? null : new IntentMatch(matched.Id, 0.85f);
    }
}
