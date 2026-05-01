using Microsoft.Extensions.Options;

namespace VoiceAgent.Infrastructure.Providers.Telephony;

public sealed class FreeSwitchTelephonyProvider(IOptions<FreeSwitchOptions> optionsAccessor)
{
    private readonly FreeSwitchOptions _options = optionsAccessor.Value;
    public Task<string> DialAsync(string phone, CancellationToken ct = default)
    {
        if (_options.UseMockProviders) return Task.FromResult("mock-call-id");
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("FreeSwitch Host is required when UseMockProviders=false.");
        throw new NotSupportedException("FreeSwitch real dial is not yet implemented. Configure Telnyx or enable mock mode.");
    }
}
