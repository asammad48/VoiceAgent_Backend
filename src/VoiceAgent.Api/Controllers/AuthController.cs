using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Auth;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration, IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var useMockProviders = configuration.GetValue<bool>("FeatureFlags:UseMockProviders", true);
        if (!useMockProviders)
        {
            return StatusCode(StatusCodes.Status501NotImplemented,
                ApiResponse<LoginResponseDto>.Fail("Auth mock login is disabled because FeatureFlags:UseMockProviders=false. Configure real auth provider."));
        }

        var login = await authService.LoginAsync(request, ct);
        if (login is null)
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail("Invalid email or password."));
        }

        return Ok(ApiResponse<LoginResponseDto>.Ok(login, "Login successful."));
    }
}
