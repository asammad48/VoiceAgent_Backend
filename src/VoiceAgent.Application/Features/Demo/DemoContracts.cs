namespace VoiceAgent.Application.Features.Demo;

public record StartConversationRequest(Guid TenantId, Guid ClientId, Guid CampaignId, string? CustomerName);
public record SendMessageRequest(Guid CallSessionId, string Message);

public record DemoTurnResponse(Guid CallSessionId, int TurnNumber, string UserMessage, string AgentMessage, string State);

public interface IDemoConversationService
{
    Task<DemoTurnResponse> StartAsync(StartConversationRequest request, CancellationToken ct = default);
    Task<DemoTurnResponse> SendAsync(SendMessageRequest request, CancellationToken ct = default);
}
