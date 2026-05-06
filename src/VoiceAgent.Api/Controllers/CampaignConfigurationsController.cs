using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.CampaignConfigurations;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignConfigurationsController(ICampaignConfigurationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateCampaignConfigurationRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateAsync(request, ct) });

    
    [HttpPatch("{id:guid}")]
    public ActionResult<ApiResponse<bool>> Update(Guid id, [FromBody] UpdateCampaignConfigurationRequestDto request)
        => Ok(id == Guid.Empty
            ? ApiResponse<bool>.Fail("Campaign configuration not found.")
            : ApiResponse<bool>.Ok(true, "Campaign configuration updated."));

    [HttpGet("by-campaign/{campaignId:guid}")]
    public ActionResult<ApiResponse<CampaignConfigurationResponseDto?>> ByCampaign(Guid campaignId)
        => Ok(new ApiResponse<CampaignConfigurationResponseDto?> { Success = true, Data = null });
}
