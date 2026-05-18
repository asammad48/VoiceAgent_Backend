using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Speech;

public sealed class DeepgramClient(HttpClient httpClient, IOptions<DeepgramOptions> optionsAccessor, ILogger<DeepgramClient> logger)
{
    private readonly DeepgramOptions _options = optionsAccessor.Value;

    public async Task<string> TranscribeAsync(byte[] audio, CancellationToken ct = default)
    {
        if (_options.UseMockProviders) return "[mock-transcript]";
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) throw new InvalidOperationException("Deepgram ApiKey is required when UseMockProviders=false.");

        logger.LogInformation("[Deepgram] STT call: audioBytes={Bytes}", audio.Length);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/v1/listen?model=nova-2&smart_format=true");
        req.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.ApiKey);
        req.Content = new ByteArrayContent(audio);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

        using var res = await httpClient.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        sw.Stop();

        if (!res.IsSuccessStatusCode)
        {
            logger.LogError("[Deepgram] STT call failed: status={Status} body={Body}", (int)res.StatusCode, json);
            throw new InvalidOperationException($"Deepgram failed: {(int)res.StatusCode} {json}");
        }

        using var doc = JsonDocument.Parse(json);
        var transcript = doc.RootElement
            .GetProperty("results")
            .GetProperty("channels")[0]
            .GetProperty("alternatives")[0]
            .GetProperty("transcript")
            .GetString();

        logger.LogInformation("[Deepgram] STT response: elapsedMs={Ms} transcript='{Transcript}'", sw.ElapsedMilliseconds, transcript);
        return transcript ?? string.Empty;
    }
}
