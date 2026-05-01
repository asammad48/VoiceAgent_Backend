using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces.Voice;
using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Application.Services.Voice;

public class VoiceSessionService(IAppDbContext db) : IVoiceSessionService
{
    public async Task<(Guid CallSessionId, string CorrelationId)> StartSessionAsync(Guid tenantId, Guid clientId, Guid campaignId, string channel, CancellationToken ct = default)
    {
        var callChannel = Enum.TryParse<CallChannel>(channel, true, out var parsed) ? parsed : CallChannel.WebText;
        var session = new CallSession
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ClientId = clientId, CampaignId = campaignId,
            Channel = callChannel, Direction = CampaignDirection.WebDemo, Status = CallStatus.InProgress,
            CurrentState = ConversationState.Greeting, CorrelationId = Guid.NewGuid().ToString("N"), StartedAt = DateTime.UtcNow
        };
        db.CallSessions.Add(session);
        db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "voice_session_started", EventDataJson = $"{{\"channel\":\"{callChannel}\"}}" });
        await db.SaveChangesAsync(ct);
        return (session.Id, session.CorrelationId);
    }

    public async Task EndSessionAsync(Guid callSessionId, CancellationToken ct = default)
    {
        var session = await db.CallSessions.FirstOrDefaultAsync(x => x.Id == callSessionId, ct);
        if (session is null) return;
        session.Status = CallStatus.Completed;
        session.EndedAt = DateTime.UtcNow;
        session.DurationSeconds = (int)Math.Max(0, (session.EndedAt.Value - session.StartedAt).TotalSeconds);
        db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "voice_session_ended" });
        await db.SaveChangesAsync(ct);
    }
}
