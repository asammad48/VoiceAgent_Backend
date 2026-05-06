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

    
    [HttpPatch("{id:guid}")]
    public ActionResult<ApiResponse<bool>> Update(Guid id, [FromBody] UpdateCampaignRequestDto request)
        => Ok(id == Guid.Empty
            ? ApiResponse<bool>.Fail("Campaign not found.")
            : ApiResponse<bool>.Ok(true, "Campaign updated."));

    [HttpGet("by-client/{clientId:guid}")]
    public ActionResult<ApiResponse<IReadOnlyList<CampaignResponseDto>>> ByClient(Guid clientId)
        => Ok(new ApiResponse<IReadOnlyList<CampaignResponseDto>> { Success = true, Data = Array.Empty<CampaignResponseDto>() });

    [HttpGet("demo")]
    public ActionResult<ApiResponse<IReadOnlyList<CampaignResponseDto>>> Demo()
        => Ok(new ApiResponse<IReadOnlyList<CampaignResponseDto>> { Success = true, Data = Array.Empty<CampaignResponseDto>() });
}
