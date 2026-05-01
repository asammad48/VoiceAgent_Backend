using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Interfaces.Voice;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/voice")]
public class VoiceController(IVoiceSessionService voiceSessionService) : ControllerBase
{
    [HttpPost("session/start")]
    public async Task<ActionResult<ApiResponse<object>>> Start([FromBody] VoiceStartRequest request, CancellationToken ct)
    {
        var (callSessionId, correlationId) = await voiceSessionService.StartSessionAsync(request.TenantId, request.ClientId, request.CampaignId, request.Channel, ct);
        return Ok(ApiResponse<object>.Ok(new { callSessionId, correlationId }, "Voice session started."));
    }

    [HttpPost("session/end")]
    public async Task<ActionResult<ApiResponse<object>>> End([FromBody] VoiceEndRequest request, CancellationToken ct)
    {
        await voiceSessionService.EndSessionAsync(request.CallSessionId, ct);
        return Ok(ApiResponse<object>.Ok(new { request.CallSessionId }, "Voice session ended."));
    }

    public sealed class VoiceStartRequest
    {
        public Guid TenantId { get; set; }
        public Guid ClientId { get; set; }
        public Guid CampaignId { get; set; }
        public string Channel { get; set; } = "WebText";
    }

    public sealed class VoiceEndRequest { public Guid CallSessionId { get; set; } }
}
