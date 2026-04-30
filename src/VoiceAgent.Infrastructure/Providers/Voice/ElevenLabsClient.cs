using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Voice;

public sealed class ElevenLabsClient(HttpClient httpClient, IOptions<ElevenLabsOptions> optionsAccessor)
{
    private readonly ElevenLabsOptions _options = optionsAccessor.Value;
    public Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
        => Task.FromResult(Array.Empty<byte>());
}
