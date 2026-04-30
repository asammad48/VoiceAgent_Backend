using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Telephony;

public sealed class TelnyxTelephonyProvider(HttpClient httpClient, IOptions<TelnyxOptions> optionsAccessor)
{
    private readonly TelnyxOptions _options = optionsAccessor.Value;
    public Task<string> DialAsync(string phone, CancellationToken ct = default)
        => Task.FromResult(_options.UseMockProviders ? "mock-telnyx-call" : string.Empty);
}
