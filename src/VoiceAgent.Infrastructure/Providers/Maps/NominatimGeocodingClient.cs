using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Maps;

public sealed class NominatimGeocodingClient(HttpClient httpClient, IOptions<NominatimOptions> optionsAccessor)
{
    private readonly NominatimOptions _options = optionsAccessor.Value;
    public Task<(double lat, double lon)> GeocodeAsync(string address, CancellationToken ct = default)
        => Task.FromResult(_options.UseMockProviders ? (40.0, -74.0) : (0.0, 0.0));
}
