using VoiceAgent.Application.Interfaces;

namespace VoiceAgent.Infrastructure.Providers;

public sealed class MockLocationNormalizationService : ILocationNormalizationService
{
    public Task<(string Pickup, string Dropoff)> NormalizeLocationsAsync(
        string pickup, string dropoff,
        CancellationToken ct = default)
        => Task.FromResult((pickup, dropoff));
}
