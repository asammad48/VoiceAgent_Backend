using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace VoiceAgent.Infrastructure.Providers.Telephony;

public sealed class TelnyxTelephonyProvider(HttpClient httpClient, IOptions<TelnyxOptions> optionsAccessor)
{
    private readonly TelnyxOptions _options = optionsAccessor.Value;
    public async Task<string> DialAsync(string phone, CancellationToken ct = default)
    {
        if (_options.UseMockProviders) return "mock-telnyx-call";
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ConnectionId) || string.IsNullOrWhiteSpace(_options.FromNumber))
            throw new InvalidOperationException("Telnyx ApiKey, ConnectionId and FromNumber are required when UseMockProviders=false.");

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v2/calls";
        var payload = JsonSerializer.Serialize(new
        {
            to = phone,
            from = _options.FromNumber,
            connection_id = _options.ConnectionId
        });
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.ApiKey}");
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var res = await httpClient.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Telnyx dial failed: {(int)res.StatusCode} {json}");
        var parsed = JsonSerializer.Deserialize<TelnyxDialResponse>(json);
        return parsed?.data?.call_control_id ?? throw new InvalidOperationException("Telnyx dial succeeded but call_control_id is missing.");
    }

    private sealed class TelnyxDialResponse { public TelnyxDialData? data { get; set; } }
    private sealed class TelnyxDialData { public string? call_control_id { get; set; } }
}
