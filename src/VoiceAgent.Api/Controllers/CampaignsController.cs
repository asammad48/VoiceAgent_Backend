using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Campaigns;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController(ICampaignService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateCampaignRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateAsync(request, ct) });

    [HttpGet("by-client/{clientId:guid}")]
    public ActionResult<ApiResponse<object>> ByClient(Guid clientId)
        => Ok(new ApiResponse<object> { Success = true, Data = new { clientId, items = Array.Empty<object>() } });

    [HttpGet("demo")]
    public ActionResult<ApiResponse<object>> Demo()
        => Ok(new ApiResponse<object> { Success = true, Data = Array.Empty<object>() });
}
