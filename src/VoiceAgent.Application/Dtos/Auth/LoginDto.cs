namespace VoiceAgent.Application.Dtos.Auth;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid? ClientId { get; set; }
}
