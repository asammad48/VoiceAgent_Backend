namespace VoiceAgent.Infrastructure.Providers.Llm;
public sealed class GeminiOptions { public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com"; public bool UseMockProviders { get; set; } public string ApiKey { get; set; } = string.Empty; }
