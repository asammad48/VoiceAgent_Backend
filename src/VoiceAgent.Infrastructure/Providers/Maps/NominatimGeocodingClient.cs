using Microsoft.Extensions.Options;
using System.Text.Json;

namespace VoiceAgent.Infrastructure.Providers.Maps;

public sealed class NominatimGeocodingClient(HttpClient httpClient, IOptions<NominatimOptions> optionsAccessor)
{
    private readonly NominatimOptions _options = optionsAccessor.Value;
    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        if (_options.UseMockProviders) return (40.0, -74.0);

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/search?format=jsonv2&limit=1&q={Uri.EscapeDataString(address)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("User-Agent", "VoiceAgentBackend/1.0");
        using var response = await httpClient.SendAsync(req, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<List<NominatimResult>>(stream, cancellationToken: ct) ?? [];
        var first = payload.FirstOrDefault();
        if (first is null) return null;
        if (!double.TryParse(first.lat, out var lat) || !double.TryParse(first.lon, out var lon)) return null;
        return (lat, lon);
    }

    private sealed class NominatimResult { public string lat { get; set; } = string.Empty; public string lon { get; set; } = string.Empty; }
}
