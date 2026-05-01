using Microsoft.Extensions.Options;
using System.Text.Json;

namespace VoiceAgent.Infrastructure.Providers.Maps;

public sealed class OsrmRoutingClient(HttpClient httpClient, IOptions<OsrmOptions> optionsAccessor)
{
    private readonly OsrmOptions _options = optionsAccessor.Value;
    public async Task<decimal?> GetDistanceKmAsync((double Latitude, double Longitude) from, (double Latitude, double Longitude) to, CancellationToken ct = default)
    {
        if (_options.UseMockProviders) return 8m;

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/route/v1/driving/{from.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{from.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)};{to.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{to.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=false";
        using var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<OsrmResponse>(stream, cancellationToken: ct);
        var meters = payload?.routes?.FirstOrDefault()?.distance;
        return meters.HasValue ? Math.Round((decimal)meters.Value / 1000m, 2) : null;
    }

    private sealed class OsrmResponse { public List<Route>? routes { get; set; } }
    private sealed class Route { public double distance { get; set; } }
}
