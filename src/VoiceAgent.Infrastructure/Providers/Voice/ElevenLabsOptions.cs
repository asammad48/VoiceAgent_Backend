namespace VoiceAgent.Infrastructure.Providers.Voice;
public sealed class ElevenLabsOptions { public string BaseUrl { get; set; } = "https://api.elevenlabs.io"; public bool UseMockProviders { get; set; } public string ApiKey { get; set; } = string.Empty; }
