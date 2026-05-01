namespace VoiceAgent.Infrastructure.Providers.Telephony;
public sealed class TelnyxOptions
{
    public bool UseMockProviders { get; set; }
    public string BaseUrl { get; set; } = "https://api.telnyx.com";
    public string ApiKey { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}
