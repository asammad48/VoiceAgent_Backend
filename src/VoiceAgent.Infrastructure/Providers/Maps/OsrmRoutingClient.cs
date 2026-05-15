using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace VoiceAgent.Infrastructure.Providers.Maps;

public sealed class OsrmRoutingClient(
    HttpClient httpClient,
    IOptions<OsrmOptions> optionsAccessor,
    ILogger<OsrmRoutingClient> logger)
{
    private readonly OsrmOptions _options = optionsAccessor.Value;

    public async Task<decimal?> GetDistanceKmAsync(
        (double Latitude, double Longitude) from,
        (double Latitude, double Longitude) to,
        CancellationToken ct = default)
    {
        if (_options.UseMockProviders)
        {
            logger.LogInformation("[OSRM] Mock mode — returning 8 km for ({FromLat},{FromLon}) → ({ToLat},{ToLon})",
                from.Latitude, from.Longitude, to.Latitude, to.Longitude);
            return 8m;
        }

        var ic = System.Globalization.CultureInfo.InvariantCulture;
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/route/v1/driving/{from.Longitude.ToString(ic)},{from.Latitude.ToString(ic)};{to.Longitude.ToString(ic)},{to.Latitude.ToString(ic)}?overview=false";

        logger.LogInformation("[OSRM] Requesting route: ({FromLat},{FromLon}) → ({ToLat},{ToLon}) — GET {Url}",
            from.Latitude, from.Longitude, to.Latitude, to.Longitude, url);

        using var response = await httpClient.GetAsync(url, ct);
        logger.LogInformation("[OSRM] HTTP {StatusCode} for route ({FromLat},{FromLon}) → ({ToLat},{ToLon})",
            (int)response.StatusCode, from.Latitude, from.Longitude, to.Latitude, to.Longitude);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<OsrmResponse>(stream, cancellationToken: ct);
        var meters = payload?.routes?.FirstOrDefault()?.distance;

        if (!meters.HasValue)
        {
            logger.LogWarning("[OSRM] No route found for ({FromLat},{FromLon}) → ({ToLat},{ToLon})",
                from.Latitude, from.Longitude, to.Latitude, to.Longitude);
            return null;
        }

        var distanceKm = Math.Round((decimal)meters.Value / 1000m, 2);
        logger.LogInformation("[OSRM] Route distance: {Meters}m → {Km} km for ({FromLat},{FromLon}) → ({ToLat},{ToLon})",
            meters.Value, distanceKm, from.Latitude, from.Longitude, to.Latitude, to.Longitude);
        return distanceKm;
    }

    private sealed class OsrmResponse { public List<Route>? routes { get; set; } }
    private sealed class Route { public double distance { get; set; } }
}
