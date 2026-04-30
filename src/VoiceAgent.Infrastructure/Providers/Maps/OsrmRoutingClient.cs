using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Maps;

public sealed class OsrmRoutingClient(HttpClient httpClient, IOptions<OsrmOptions> optionsAccessor)
{
    private readonly OsrmOptions _options = optionsAccessor.Value;
    public Task<decimal> GetDistanceKmAsync((double lat, double lon) from, (double lat, double lon) to, CancellationToken ct = default)
        => Task.FromResult(_options.UseMockProviders ? 8m : 0m);
}
