using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace VoiceAgent.Infrastructure.Providers.Maps;

public sealed class NominatimGeocodingClient(
    HttpClient httpClient,
    IOptions<NominatimOptions> optionsAccessor,
    ILogger<NominatimGeocodingClient> logger)
{
    private readonly NominatimOptions _options = optionsAccessor.Value;

    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            logger.LogWarning("[Nominatim] GeocodeAsync called with empty address — returning null");
            return null;
        }

        if (_options.UseMockProviders)
        {
            logger.LogInformation("[Nominatim] Mock mode — returning (40.0, -74.0) for address: \"{Address}\"", address);
            return (40.0, -74.0);
        }

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/search?format=jsonv2&limit=1&q={Uri.EscapeDataString(address)}";
        logger.LogInformation("[Nominatim] Geocoding address: \"{Address}\" → GET {Url}", address, url);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("User-Agent", "VoiceAgentBackend/1.0");

        using var response = await httpClient.SendAsync(req, ct);
        logger.LogInformation("[Nominatim] HTTP {StatusCode} for \"{Address}\"", (int)response.StatusCode, address);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<List<NominatimResult>>(stream, cancellationToken: ct) ?? [];

        if (payload.Count == 0)
        {
            logger.LogWarning("[Nominatim] No results returned for address: \"{Address}\"", address);
            return null;
        }

        var first = payload[0];
        logger.LogInformation("[Nominatim] Raw result for \"{Address}\": lat={Lat}, lon={Lon}, display_name={DisplayName}",
            address, first.lat, first.lon, first.display_name);

        if (!double.TryParse(first.lat, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(first.lon, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var lon))
        {
            logger.LogError("[Nominatim] Failed to parse lat/lon for \"{Address}\": lat=\"{Lat}\" lon=\"{Lon}\"",
                address, first.lat, first.lon);
            return null;
        }

        logger.LogInformation("[Nominatim] Geocoded \"{Address}\" → ({Lat}, {Lon})", address, lat, lon);
        return (lat, lon);
    }

    private sealed class NominatimResult
    {
        public string lat { get; set; } = string.Empty;
        public string lon { get; set; } = string.Empty;
        public string display_name { get; set; } = string.Empty;
    }
}
