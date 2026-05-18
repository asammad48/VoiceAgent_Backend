using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Voice;

public sealed class ElevenLabsClient(HttpClient httpClient, IOptions<ElevenLabsOptions> optionsAccessor, ILogger<ElevenLabsClient> logger)
{
    private readonly ElevenLabsOptions _options = optionsAccessor.Value;

    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
    {
        if (_options.UseMockProviders) return Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) throw new InvalidOperationException("ElevenLabs ApiKey is required when UseMockProviders=false.");

        logger.LogInformation("[ElevenLabs] TTS call: voiceId={VoiceId} textLen={Len} preview='{Preview}'",
            _options.DefaultVoiceId, text.Length, text.Length > 60 ? text[..60] : text);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/v1/text-to-speech/{_options.DefaultVoiceId}/stream");
        req.Headers.Add("xi-api-key", _options.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(new
        {
            text,
            model_id = "eleven_turbo_v2_5",
            output_format = "mp3_44100_128"
        }), Encoding.UTF8, "application/json");

        using var res = await httpClient.SendAsync(req, ct);
        var bytes = await res.Content.ReadAsByteArrayAsync(ct);
        sw.Stop();

        if (!res.IsSuccessStatusCode)
        {
            logger.LogError("[ElevenLabs] TTS call failed: status={Status} body={Body}", (int)res.StatusCode, Encoding.UTF8.GetString(bytes));
            throw new InvalidOperationException($"ElevenLabs failed: {(int)res.StatusCode} {Encoding.UTF8.GetString(bytes)}");
        }

        logger.LogInformation("[ElevenLabs] TTS response: elapsedMs={Ms} audioBytes={Bytes}", sw.ElapsedMilliseconds, bytes.Length);
        return bytes;
    }
}
