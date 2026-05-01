using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Calls;
using VoiceAgent.Application.Interfaces;

namespace VoiceAgent.Application.Services;

public class CallQueryService(IAppDbContext db) : ICallQueryService
{
    public async Task<CallSessionResponseDto?> GetSessionAsync(Guid callSessionId, CancellationToken ct = default)
    {
        return await db.CallSessions
            .Where(x => x.Id == callSessionId)
            .Select(x => new CallSessionResponseDto
            {
                Id = x.Id,
                Status = x.Status.ToString(),
                CurrentState = x.CurrentState.ToString(),
                StartedAt = x.StartedAt
            })
            .FirstOrDefaultAsync(ct);
    }
}
