using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Storage;

public sealed class CloudflareR2StorageClient(HttpClient httpClient, IOptions<CloudflareR2Options> optionsAccessor)
{
    private readonly CloudflareR2Options _options = optionsAccessor.Value;
    public Task<string> PutObjectAsync(string key, byte[] payload, CancellationToken ct = default)
        => Task.FromResult(_options.UseMockProviders ? $"mock://{key}" : key);
}
