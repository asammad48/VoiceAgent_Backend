using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Recordings;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordingsController(IRecordingService service) : ControllerBase
{
    [HttpPost("upload-url")]
    public async Task<ActionResult<ApiResponse<RecordingUrlDto>>> CreateUploadUrl([FromBody] CreateRecordingUploadUrlRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<RecordingUrlDto> { Success = true, Data = await service.CreateUploadUrlAsync(request, ct) });

    [HttpGet("{recordingId:guid}/read-url")]
    public async Task<ActionResult<ApiResponse<RecordingUrlDto>>> CreateReadUrl(Guid recordingId, CancellationToken ct)
    {
        var result = await service.CreateReadUrlAsync(recordingId, ct);
        if (result is null) return NotFound(new ApiResponse<RecordingUrlDto> { Success = false, Message = "Recording not found." });
        return Ok(new ApiResponse<RecordingUrlDto> { Success = true, Data = result });
    }
}
