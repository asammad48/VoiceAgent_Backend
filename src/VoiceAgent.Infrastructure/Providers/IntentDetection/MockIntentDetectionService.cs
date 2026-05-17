using VoiceAgent.Application.Interfaces;

namespace VoiceAgent.Infrastructure.Providers.IntentDetection;

public class MockIntentDetectionService : IIntentDetectionService
{
    public Task<IntentMatch?> DetectAsync(string userMessage, IReadOnlyList<IntentTrigger> intents, CancellationToken ct = default)
    {
        // In mock mode always run keyword matching; fallback to first non-transfer intent
        var keyword = KeywordIntentDetectionService.TryKeywordMatch(userMessage, intents);
        if (keyword is not null) return Task.FromResult<IntentMatch?>(keyword);

        var fallback = intents.FirstOrDefault(i => i.Type != "transfer");
        return Task.FromResult(fallback is null ? null : new IntentMatch(fallback.Id, 0.5f));
    }
}
