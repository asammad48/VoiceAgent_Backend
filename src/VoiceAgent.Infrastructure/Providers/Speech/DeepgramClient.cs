using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Speech;

public sealed class DeepgramClient(HttpClient httpClient, IOptions<DeepgramOptions> optionsAccessor)
{
    private readonly DeepgramOptions _options = optionsAccessor.Value;
    public Task<string> TranscribeAsync(byte[] audio, CancellationToken ct = default)
        => Task.FromResult(_options.UseMockProviders ? "[mock-transcript]" : string.Empty);
}
