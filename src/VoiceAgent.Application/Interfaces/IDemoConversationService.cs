using VoiceAgent.Application.Dtos.Campaigns;
using VoiceAgent.Application.Dtos.Calls;
using VoiceAgent.Application.Dtos.Demo;

namespace VoiceAgent.Application.Interfaces;

public interface IDemoConversationService
{
    Task<IReadOnlyList<CampaignResponseDto>> GetDemoCampaignsAsync(CancellationToken ct = default);
    Task<StartDemoConversationResponseDto> StartAsync(StartDemoConversationRequestDto request, CancellationToken ct = default);
    Task<SendDemoMessageResponseDto> SendAsync(SendDemoMessageRequestDto request, CancellationToken ct = default);
    Task<bool> EndAsync(Guid callSessionId, CancellationToken ct = default);
    Task<CallSessionResponseDto?> GetSessionAsync(Guid callSessionId, CancellationToken ct = default);
}
