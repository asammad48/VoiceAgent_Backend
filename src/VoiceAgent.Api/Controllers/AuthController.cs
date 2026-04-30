using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public ActionResult<ApiResponse<object>> Login([FromBody] object request)
        => Ok(new ApiResponse<object> { Success = true, Data = new { token = "demo-token" } });
}
