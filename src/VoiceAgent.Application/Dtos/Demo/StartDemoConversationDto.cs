namespace VoiceAgent.Application.Dtos.Demo;
public class StartDemoConversationRequestDto { public Guid TenantId { get; set; } public Guid ClientId { get; set; } public Guid CampaignId { get; set; } public string Channel { get; set; } = string.Empty; }
public class StartDemoConversationResponseDto { public Guid CallSessionId { get; set; } public string Message { get; set; } = string.Empty; public string CurrentState { get; set; } = string.Empty; }
