using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Voice;

public sealed class ElevenLabsClient(HttpClient httpClient, IOptions<ElevenLabsOptions> optionsAccessor)
{
    private readonly ElevenLabsOptions _options = optionsAccessor.Value;

    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
    {
        if (_options.UseMockProviders) return Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) throw new InvalidOperationException("ElevenLabs ApiKey is required when UseMockProviders=false.");

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/v1/text-to-speech/EXAVITQu4vr4xnSDxMaL/stream");
        req.Headers.Add("xi-api-key", _options.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(new
        {
            text,
            model_id = "eleven_turbo_v2_5",
            output_format = "mp3_44100_128"
        }), Encoding.UTF8, "application/json");

        using var res = await httpClient.SendAsync(req, ct);
        var bytes = await res.Content.ReadAsByteArrayAsync(ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"ElevenLabs failed: {(int)res.StatusCode} {Encoding.UTF8.GetString(bytes)}");
        return bytes;
    }
}
