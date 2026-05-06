using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Health;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(IHostEnvironment hostEnvironment) : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<HealthResponseDto>> Get()
        => Ok(CreateResponse("Healthy"));

    [HttpGet("ready")]
    public ActionResult<ApiResponse<HealthResponseDto>> Ready()
        => Ok(CreateResponse("Ready"));

    [HttpGet("live")]
    public ActionResult<ApiResponse<HealthResponseDto>> Live()
        => Ok(CreateResponse("Live"));

    private ApiResponse<HealthResponseDto> CreateResponse(string status)
        => new()
        {
            Success = true,
            Data = new HealthResponseDto
            {
                Status = status,
                Service = "VoiceAgent.Api",
                Environment = hostEnvironment.EnvironmentName,
                TimestampUtc = DateTime.UtcNow
            }
        };
}
