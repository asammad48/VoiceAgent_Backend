using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Branches;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BranchesController(IBranchService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateBranchRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateAsync(request, ct) });

    
    [HttpPatch("{id:guid}")]
    public ActionResult<ApiResponse<bool>> Update(Guid id, [FromBody] UpdateBranchRequestDto request)
        => Ok(id == Guid.Empty
            ? ApiResponse<bool>.Fail("Branch not found.")
            : ApiResponse<bool>.Ok(true, "Branch updated."));

    [HttpGet("by-client/{clientId:guid}")]
    public ActionResult<ApiResponse<IReadOnlyList<BranchResponseDto>>> ByClient(Guid clientId)
        => Ok(new ApiResponse<IReadOnlyList<BranchResponseDto>> { Success = true, Data = Array.Empty<BranchResponseDto>() });
}
