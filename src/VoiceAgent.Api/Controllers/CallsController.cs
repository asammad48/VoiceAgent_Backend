using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Calls;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CallsController(ICallQueryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CallSessionResponseDto>>>> Get([FromQuery] int limit = 50, CancellationToken ct = default)
        => Ok(ApiResponse<IReadOnlyList<CallSessionResponseDto>>.Ok(await service.GetRecentSessionsAsync(limit, ct), "Calls loaded."));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CallSessionResponseDto?>>> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<CallSessionResponseDto?>.Ok(await service.GetSessionAsync(id, ct), "Call session loaded."));

    [HttpGet("{id:guid}/turns")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CallTurnResponseDto>>>> Turns(Guid id, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<CallTurnResponseDto>>.Ok(await service.GetTurnsAsync(id, ct), "Call turns loaded."));

    [HttpGet("{id:guid}/events")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CallEventResponseDto>>>> Events(Guid id, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<CallEventResponseDto>>.Ok(await service.GetEventsAsync(id, ct), "Call events loaded."));

    [HttpGet("{id:guid}/tool-logs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ToolCallLogResponseDto>>>> ToolLogs(Guid id, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<ToolCallLogResponseDto>>.Ok(await service.GetToolLogsAsync(id, ct), "Tool logs loaded."));
}
