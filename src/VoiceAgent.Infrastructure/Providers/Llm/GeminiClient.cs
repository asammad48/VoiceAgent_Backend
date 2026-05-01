using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Llm;

public sealed class GeminiClient(HttpClient httpClient, IOptions<GeminiOptions> optionsAccessor)
{
    private readonly GeminiOptions _options = optionsAccessor.Value;

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        if (_options.UseMockProviders) return "[mock-gemini]";
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) throw new InvalidOperationException("Gemini ApiKey is required when UseMockProviders=false.");

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v1beta/models/gemini-1.5-flash:generateContent?key={_options.ApiKey}";
        var body = JsonSerializer.Serialize(new { contents = new[] { new { parts = new[] { new { text = prompt } } } } });
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        using var res = await httpClient.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Gemini failed: {(int)res.StatusCode} {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;
    }
}
