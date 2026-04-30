namespace VoiceAgent.Infrastructure.Providers.Storage;
public sealed class CloudflareR2Options { public bool UseMockProviders { get; set; } public string Endpoint { get; set; } = string.Empty; public string Bucket { get; set; } = string.Empty; }
