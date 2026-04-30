using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Llm;

public sealed class GeminiClient(HttpClient httpClient, IOptions<GeminiOptions> optionsAccessor)
{
    private readonly GeminiOptions _options = optionsAccessor.Value;
    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
        => _options.UseMockProviders ? "[mock-gemini]" : await httpClient.GetStringAsync(_options.BaseUrl, ct);
}
