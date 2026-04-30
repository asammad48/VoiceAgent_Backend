using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Telephony;

public sealed class FreeSwitchTelephonyProvider(IOptions<FreeSwitchOptions> optionsAccessor)
{
    private readonly FreeSwitchOptions _options = optionsAccessor.Value;
    public Task<string> DialAsync(string phone, CancellationToken ct = default)
        => Task.FromResult(_options.UseMockProviders ? "mock-call-id" : string.Empty);
}
