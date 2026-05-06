using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Outbound;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Pagination;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/outbound")]
public class OutboundController(IOutboundService service) : ControllerBase
{
    [HttpPost("runs")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateRun([FromBody] CreateOutboundRunRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateRunAsync(request, ct) });

    [HttpPost("runs/search")]
    public async Task<ActionResult<ApiResponse<PagedResponseDto<OutboundRunDto>>>> SearchRuns([FromBody] PagedRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<PagedResponseDto<OutboundRunDto>> { Success = true, Data = await service.SearchRunsAsync(request, ct) });

    [HttpPost("runs/{runId:guid}/leads")]
    public async Task<ActionResult<ApiResponse<Guid>>> UpsertLead(Guid runId, [FromBody] UpsertOutboundLeadRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.UpsertLeadAsync(runId, request, ct) });

    [HttpPost("runs/{runId:guid}/leads/search")]
    public async Task<ActionResult<ApiResponse<PagedResponseDto<OutboundLeadDto>>>> SearchLeads(Guid runId, [FromBody] PagedRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<PagedResponseDto<OutboundLeadDto>> { Success = true, Data = await service.SearchLeadsAsync(runId, request, ct) });
}
