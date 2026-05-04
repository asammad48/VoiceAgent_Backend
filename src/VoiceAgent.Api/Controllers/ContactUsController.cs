using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.ContactUs;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactUsController(IContactUsService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateContactUsRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateAsync(request, ct) });

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContactUsResponseDto>>>> GetAll(CancellationToken ct)
        => Ok(new ApiResponse<IReadOnlyList<ContactUsResponseDto>> { Success = true, Data = await service.GetAllAsync(ct) });

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ContactUsResponseDto>>> GetById(Guid id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        if (item is null)
        {
            return NotFound(new ApiResponse<ContactUsResponseDto> { Success = false, Message = "ContactUs message not found." });
        }

        return Ok(new ApiResponse<ContactUsResponseDto> { Success = true, Data = item });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(Guid id, [FromBody] UpdateContactUsStatusRequestDto request, CancellationToken ct)
    {
        var updated = await service.UpdateStatusAsync(id, request.Status, ct);
        if (!updated)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = "ContactUs message not found." });
        }

        return Ok(new ApiResponse<object> { Success = true, Data = new { id, status = request.Status } });
    }

    [HttpGet("status-summary")]
    public async Task<ActionResult<ApiResponse<ContactUsStatusSummaryDto>>> GetStatusSummary(CancellationToken ct)
        => Ok(new ApiResponse<ContactUsStatusSummaryDto> { Success = true, Data = await service.GetStatusSummaryAsync(ct) });
}

