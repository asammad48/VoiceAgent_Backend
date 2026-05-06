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

    
    [HttpPatch("{id:guid}")]
    public ActionResult<ApiResponse<bool>> Update(Guid id, [FromBody] UpdateClientRequestDto request)
        => Ok(id == Guid.Empty
            ? ApiResponse<bool>.Fail("Client not found.")
            : ApiResponse<bool>.Ok(true, "Client updated."));

    [HttpGet("by-tenant/{tenantId:guid}")]
    public ActionResult<ApiResponse<IReadOnlyList<ClientResponseDto>>> ByTenant(Guid tenantId)
        => Ok(new ApiResponse<IReadOnlyList<ClientResponseDto>> { Success = true, Data = Array.Empty<ClientResponseDto>() });
}
