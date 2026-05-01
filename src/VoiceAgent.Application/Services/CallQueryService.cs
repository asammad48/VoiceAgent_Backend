using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Calls;
using VoiceAgent.Application.Interfaces;

namespace VoiceAgent.Application.Services;

public class CallQueryService(IAppDbContext db) : ICallQueryService
{
    public async Task<IReadOnlyList<CallSessionResponseDto>> GetRecentSessionsAsync(int limit = 50, CancellationToken ct = default)
    {
        var take = Math.Clamp(limit, 1, 200);
        return await db.CallSessions
            .OrderByDescending(x => x.StartedAt)
            .Take(take)
            .Select(x => new CallSessionResponseDto
            {
                Id = x.Id,
                Status = x.Status.ToString(),
                CurrentState = x.CurrentState.ToString(),
                StartedAt = x.StartedAt,
                EndedAt = x.EndedAt,
                CorrelationId = x.CorrelationId,
                CollectedSlotsJson = x.CollectedSlotsJson,
                FinalResultJson = x.FinalResultJson
            })
            .ToListAsync(ct);
    }

    public async Task<CallSessionResponseDto?> GetSessionAsync(Guid callSessionId, CancellationToken ct = default)
    {
        return await db.CallSessions
            .Where(x => x.Id == callSessionId)
            .Select(x => new CallSessionResponseDto
            {
                Id = x.Id,
                Status = x.Status.ToString(),
                CurrentState = x.CurrentState.ToString(),
                StartedAt = x.StartedAt,
                EndedAt = x.EndedAt,
                CorrelationId = x.CorrelationId,
                CollectedSlotsJson = x.CollectedSlotsJson,
                FinalResultJson = x.FinalResultJson
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<CallTurnResponseDto>> GetTurnsAsync(Guid callSessionId, CancellationToken ct = default)
    {
        return await db.CallTurns
            .Where(x => x.CallSessionId == callSessionId)
            .OrderBy(x => x.TurnNumber)
            .ThenBy(x => x.CreatedOn)
            .Select(x => new CallTurnResponseDto
            {
                Id = x.Id,
                TurnNumber = x.TurnNumber,
                Speaker = x.Speaker,
                Text = x.Text,
                CreatedOn = x.CreatedOn
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CallEventResponseDto>> GetEventsAsync(Guid callSessionId, CancellationToken ct = default)
    {
        return await db.CallEvents
            .Where(x => x.CallSessionId == callSessionId)
            .OrderBy(x => x.CreatedOn)
            .Select(x => new CallEventResponseDto
            {
                Id = x.Id,
                EventType = x.EventType,
                EventDataJson = x.EventDataJson,
                CreatedOn = x.CreatedOn
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ToolCallLogResponseDto>> GetToolLogsAsync(Guid callSessionId, CancellationToken ct = default)
    {
        return await db.ToolCallLogs
            .Where(x => x.CallSessionId == callSessionId)
            .OrderBy(x => x.CreatedOn)
            .Select(x => new ToolCallLogResponseDto
            {
                Id = x.Id,
                ToolName = x.ToolName,
                Status = x.Status,
                DurationMs = x.DurationMs,
                CreatedOn = x.CreatedOn
            })
            .ToListAsync(ct);
    }
}
