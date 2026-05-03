using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Clients;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController(IClientService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateClientRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateAsync(request, ct) });

    [HttpGet("by-tenant/{tenantId:guid}")]
    public ActionResult<ApiResponse<object>> ByTenant(Guid tenantId)
        => Ok(new ApiResponse<object> { Success = true, Data = new { tenantId, items = Array.Empty<object>() } });
}
