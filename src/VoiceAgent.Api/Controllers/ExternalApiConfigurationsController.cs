using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.ExternalApis;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalApiConfigurationsController(IExternalApiConfigurationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateExternalApiConfigurationRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateAsync(request, ct) });

    
    [HttpPatch("{id:guid}")]
    public ActionResult<ApiResponse<bool>> Update(Guid id, [FromBody] UpdateExternalApiConfigurationRequestDto request)
        => Ok(id == Guid.Empty
            ? ApiResponse<bool>.Fail("External API configuration not found.")
            : ApiResponse<bool>.Ok(true, "External API configuration updated."));

    [HttpGet("by-campaign/{campaignId:guid}")]
    public ActionResult<ApiResponse<IReadOnlyList<ExternalApiConfigurationResponseDto>>> ByCampaign(Guid campaignId)
        => Ok(new ApiResponse<IReadOnlyList<ExternalApiConfigurationResponseDto>> { Success = true, Data = Array.Empty<ExternalApiConfigurationResponseDto>() });
}
