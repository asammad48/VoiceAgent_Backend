using System.Text;
using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Auth;
using VoiceAgent.Common.Responses;
using VoiceAgent.Infrastructure.Persistence.Seed;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public ActionResult<ApiResponse<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var useMockProviders = configuration.GetValue<bool>("FeatureFlags:UseMockProviders", true);
        if (!useMockProviders)
        {
            return StatusCode(StatusCodes.Status501NotImplemented,
                ApiResponse<LoginResponseDto>.Fail("Auth mock login is disabled because FeatureFlags:UseMockProviders=false. Configure real auth provider."));
        }
        var user = AuthSeed.Users.FirstOrDefault(x =>
            string.Equals(x.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
            x.Password == request.Password);

        if (user is null)
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail("Invalid email or password."));
        }

        var tokenPayload = $"{user.Email}|{user.Role}|{user.TenantId}|{user.ClientId}";
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenPayload));

        return Ok(ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
        {
            Token = token,
            Role = user.Role.ToString(),
            TenantId = user.TenantId,
            ClientId = user.ClientId
        }, "Login successful."));
    }
}
