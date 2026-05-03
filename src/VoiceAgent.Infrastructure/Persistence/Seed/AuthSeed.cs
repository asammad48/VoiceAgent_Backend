using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class AuthSeed
{
    public static readonly IReadOnlyList<SeedUser> Users =
    [
        new(
            Email: "superadmin@demo.voiceagent.local",
            Password: "Demo123!",
            Role: UserPlatformRole.SuperAdmin,
            TenantId: SeedIds.Tenant,
            ClientId: null),
        new(
            Email: "client.restaurant@demo.voiceagent.local",
            Password: "Demo123!",
            Role: UserPlatformRole.Client,
            TenantId: SeedIds.Tenant,
            ClientId: SeedIds.RestaurantClient)
    ];
}

public sealed record SeedUser(
    string Email,
    string Password,
    UserPlatformRole Role,
    Guid TenantId,
    Guid? ClientId);
