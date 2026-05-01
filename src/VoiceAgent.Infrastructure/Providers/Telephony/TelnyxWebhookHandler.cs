namespace VoiceAgent.Infrastructure.Providers.Telephony;

public class TelnyxWebhookHandler
{
    public Task<string> HandleAsync(string payload, CancellationToken ct = default)
        => Task.FromResult("accepted");
}
