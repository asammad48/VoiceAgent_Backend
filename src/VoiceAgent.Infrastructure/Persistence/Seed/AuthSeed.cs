using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class AuthSeed
{
    public static readonly IReadOnlyList<SeedUser> Users =
    [
        new(
            Id: Guid.Parse("20000000-0000-0000-0000-000000000901"),
            Email: "superadmin@demo.voiceagent.local",
            Password: "Demo123!",
            Role: UserPlatformRole.SuperAdmin,
            TenantId: SeedIds.Tenant,
            ClientId: null),
        new(
            Id: Guid.Parse("20000000-0000-0000-0000-000000000902"),
            Email: "client.restaurant@demo.voiceagent.local",
            Password: "Demo123!",
            Role: UserPlatformRole.Client,
            TenantId: SeedIds.Tenant,
            ClientId: SeedIds.RestaurantClient)
    ];

    public static IReadOnlyList<PlatformUser> AsEntities() =>
        Users.Select(u => new PlatformUser
        {
            Id = u.Id,
            Email = u.Email,
            Password = u.Password,
            Role = u.Role,
            TenantId = u.TenantId,
            ClientId = u.ClientId,
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        }).ToList();
}

public sealed record SeedUser(
    Guid Id,
    string Email,
    string Password,
    UserPlatformRole Role,
    Guid TenantId,
    Guid? ClientId);
