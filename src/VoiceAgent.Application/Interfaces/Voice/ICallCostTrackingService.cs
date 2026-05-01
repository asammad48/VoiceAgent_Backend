namespace VoiceAgent.Application.Interfaces.Voice;
public interface ICallCostTrackingService { Task TrackSttSecondsAsync(Guid callSessionId, int seconds, CancellationToken ct = default); Task TrackTtsCharsAsync(Guid callSessionId, int chars, CancellationToken ct = default); Task TrackLlmTokensAsync(Guid callSessionId, int inputTokens, int outputTokens, CancellationToken ct = default); }
