using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Features.Demo;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/demo")]
public class DemoController(IDemoConversationService service) : ControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<ApiResponse<DemoTurnResponse>>> Start([FromBody] StartConversationRequest request, CancellationToken ct)
    {
        var result = await service.StartAsync(request, ct);
        return Ok(new ApiResponse<DemoTurnResponse> { Success = true, Data = result });
    }

    [HttpPost("message")]
    public async Task<ActionResult<ApiResponse<DemoTurnResponse>>> Message([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var result = await service.SendAsync(request, ct);
        return Ok(new ApiResponse<DemoTurnResponse> { Success = true, Data = result });
    }
}
