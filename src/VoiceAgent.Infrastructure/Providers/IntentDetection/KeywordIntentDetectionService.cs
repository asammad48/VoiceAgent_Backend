using VoiceAgent.Application.Interfaces;

namespace VoiceAgent.Infrastructure.Providers.IntentDetection;

public class KeywordIntentDetectionService : IIntentDetectionService
{
    public Task<IntentMatch?> DetectAsync(string userMessage, IReadOnlyList<IntentTrigger> intents, CancellationToken ct = default)
        => Task.FromResult(TryKeywordMatch(userMessage, intents));

    // Shared by MockIntentDetectionService
    public static IntentMatch? TryKeywordMatch(string userMessage, IReadOnlyList<IntentTrigger> intents)
    {
        var lower = userMessage.ToLowerInvariant();
        var matches = new List<IntentTrigger>();

        // Transfer intents get priority — check them first
        foreach (var intent in intents.Where(i => i.Type == "transfer"))
        {
            if (intent.Triggers.Any(t => lower.Contains(t.ToLowerInvariant())))
                return new IntentMatch(intent.Id, 1.0f);
        }

        foreach (var intent in intents.Where(i => i.Type != "transfer"))
        {
            if (intent.Triggers.Any(t => lower.Contains(t.ToLowerInvariant())))
                matches.Add(intent);
        }

        return matches.Count == 1 ? new IntentMatch(matches[0].Id, 1.0f) : null;
    }
}
