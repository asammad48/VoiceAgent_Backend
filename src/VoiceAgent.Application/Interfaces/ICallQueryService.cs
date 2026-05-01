using VoiceAgent.Application.Dtos.Calls;

namespace VoiceAgent.Application.Interfaces;

public interface ICallQueryService
{
    Task<IReadOnlyList<CallSessionResponseDto>> GetRecentSessionsAsync(int limit = 50, CancellationToken ct = default);
    Task<CallSessionResponseDto?> GetSessionAsync(Guid callSessionId, CancellationToken ct = default);
    Task<IReadOnlyList<CallTurnResponseDto>> GetTurnsAsync(Guid callSessionId, CancellationToken ct = default);
    Task<IReadOnlyList<CallEventResponseDto>> GetEventsAsync(Guid callSessionId, CancellationToken ct = default);
    Task<IReadOnlyList<ToolCallLogResponseDto>> GetToolLogsAsync(Guid callSessionId, CancellationToken ct = default);
}
