using System.Text;
using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Auth;
using VoiceAgent.Application.Interfaces;

namespace VoiceAgent.Application.Services;

public class AuthService(IAppDbContext db) : IAuthService
{
    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var user = await db.PlatformUsers.FirstOrDefaultAsync(x =>
            x.IsActive &&
            string.Equals(x.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
            x.Password == request.Password, ct);

        if (user is null)
        {
            return null;
        }

        var tokenPayload = $"{user.Email}|{user.Role}|{user.TenantId}|{user.ClientId}";
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenPayload));

        return new LoginResponseDto
        {
            Token = token,
            Role = user.Role.ToString(),
            TenantId = user.TenantId,
            ClientId = user.ClientId
        };
    }
}
