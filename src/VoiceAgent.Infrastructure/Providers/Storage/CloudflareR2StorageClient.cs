using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Storage;

public sealed class CloudflareR2StorageClient(HttpClient httpClient, IOptions<CloudflareR2Options> optionsAccessor)
{
    private readonly CloudflareR2Options _options = optionsAccessor.Value;
    public async Task<string> PutObjectAsync(string key, byte[] payload, CancellationToken ct = default)
    {
        if (_options.UseMockProviders) return $"mock://{key}";
        if (string.IsNullOrWhiteSpace(_options.Endpoint) || string.IsNullOrWhiteSpace(_options.Bucket))
            throw new InvalidOperationException("CloudflareR2 Endpoint and Bucket are required when UseMockProviders=false.");

        var normalized = key.TrimStart('/');
        var url = $"{_options.Endpoint.TrimEnd('/')}/{_options.Bucket}/{normalized}";
        using var response = await httpClient.PutAsync(url, new ByteArrayContent(payload), ct);
        response.EnsureSuccessStatusCode();
        return url;
    }
}
