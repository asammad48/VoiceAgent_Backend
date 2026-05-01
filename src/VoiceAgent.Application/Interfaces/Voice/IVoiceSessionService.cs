namespace VoiceAgent.Application.Interfaces.Voice;

public interface IVoiceSessionService
{
    Task<(Guid CallSessionId, string CorrelationId)> StartSessionAsync(Guid tenantId, Guid clientId, Guid campaignId, string channel, CancellationToken ct = default);
    Task EndSessionAsync(Guid callSessionId, CancellationToken ct = default);
}
