using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Tenants;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController(ITenantService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateTenantRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateAsync(request, ct) });

    [HttpGet]
    public ActionResult<ApiResponse<IReadOnlyList<TenantResponseDto>>> Get()
        => Ok(new ApiResponse<IReadOnlyList<TenantResponseDto>> { Success = true, Data = Array.Empty<TenantResponseDto>() });

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Update([FromRoute] Guid id, [FromBody] UpdateTenantRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<bool> { Success = true, Data = await service.UpdateAsync(id, request, ct) });
}
