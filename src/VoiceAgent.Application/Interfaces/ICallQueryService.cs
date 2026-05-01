using VoiceAgent.Application.Dtos.Calls;

namespace VoiceAgent.Application.Interfaces;

public interface ICallQueryService
{
    Task<CallSessionResponseDto?> GetSessionAsync(Guid callSessionId, CancellationToken ct = default);
}
