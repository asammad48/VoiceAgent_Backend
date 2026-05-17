namespace VoiceAgent.Application.Interfaces;

public sealed record IntentTrigger(string Id, string Name, string Type, IReadOnlyList<string> Triggers);
public sealed record IntentMatch(string IntentId, float Confidence);

public interface IIntentDetectionService
{
    Task<IntentMatch?> DetectAsync(
        string userMessage,
        IReadOnlyList<IntentTrigger> intents,
        CancellationToken ct = default);
}
