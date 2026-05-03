using VoiceAgent.Application.Dtos.Auth;

namespace VoiceAgent.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
}
