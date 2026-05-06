using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Campaigns;
using VoiceAgent.Application.Dtos.Calls;
using VoiceAgent.Application.Dtos.Demo;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/demo")]
public class DemoController(IDemoConversationService demoService) : ControllerBase
{
    [HttpGet("campaigns")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CampaignResponseDto>>>> Campaigns(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<CampaignResponseDto>>.Ok(await demoService.GetDemoCampaignsAsync(ct), "Demo campaigns loaded."));

    [HttpPost("start")]
    public async Task<ActionResult<ApiResponse<StartDemoConversationResponseDto>>> Start([FromBody] StartDemoConversationRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<StartDemoConversationResponseDto>.Ok(await demoService.StartAsync(request, ct), "Demo conversation started."));

    [HttpPost("message")]
    public async Task<ActionResult<ApiResponse<SendDemoMessageResponseDto>>> Message([FromBody] SendDemoMessageRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<SendDemoMessageResponseDto>.Ok(await demoService.SendAsync(request, ct), "Message processed."));

    [HttpPost("end")]
    public async Task<ActionResult<ApiResponse<EndDemoConversationResponseDto>>> End([FromBody] EndDemoConversationRequestDto request, CancellationToken ct)
    {
        var ended = await demoService.EndAsync(request.CallSessionId, ct);
        return Ok(ended
            ? ApiResponse<EndDemoConversationResponseDto>.Ok(new EndDemoConversationResponseDto { CallSessionId = request.CallSessionId, Status = "Completed" }, "Demo conversation ended.")
            : ApiResponse<EndDemoConversationResponseDto>.Fail("Call session not found."));
    }

    [HttpGet("{callSessionId:guid}")]
    public async Task<ActionResult<ApiResponse<CallSessionResponseDto?>>> Get(Guid callSessionId, CancellationToken ct)
        => Ok(ApiResponse<CallSessionResponseDto?>.Ok(await demoService.GetSessionAsync(callSessionId, ct), "Call session loaded."));
}
