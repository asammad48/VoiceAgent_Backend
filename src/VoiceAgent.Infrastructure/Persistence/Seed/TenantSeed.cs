using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class TenantSeed
{
    public static readonly Tenant DemoTenant = new()
    {
        Id = SeedIds.Tenant,
        Name = "Demo Voice Agent Tenant",
        Slug = "demo-voice-agent",
        DefaultTimezone = "Europe/London",
        DefaultCurrency = "GBP",
        IsActive = true
    };
}
