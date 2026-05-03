using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Deals;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DealsController(IRestaurantDealService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateDealRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateDealAsync(request, ct) });

    [HttpGet("by-client/{clientId:guid}")]
    public ActionResult<ApiResponse<object>> ByClient(Guid clientId)
        => Ok(new ApiResponse<object> { Success = true, Data = new { clientId, items = Array.Empty<object>() } });
}
