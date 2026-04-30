namespace VoiceAgent.Infrastructure.Providers.Speech;
public sealed class DeepgramOptions { public string BaseUrl { get; set; } = "https://api.deepgram.com"; public bool UseMockProviders { get; set; } public string ApiKey { get; set; } = string.Empty; }
