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

    [HttpGet("by-campaign/{campaignId:guid}")]
    public ActionResult<ApiResponse<object>> ByCampaign(Guid campaignId)
        => Ok(new ApiResponse<object> { Success = true, Data = new { campaignId, items = Array.Empty<object>() } });
}
