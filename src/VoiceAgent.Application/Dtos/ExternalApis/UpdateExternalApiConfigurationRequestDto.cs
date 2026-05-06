namespace VoiceAgent.Application.Dtos.ExternalApis;

public sealed class UpdateExternalApiConfigurationRequestDto
{
    public string? Name { get; set; }
    public string? BaseUrl { get; set; }
    public string? AuthType { get; set; }
    public string? HeadersJson { get; set; }
    public string? EndpointsJson { get; set; }
    public string? SecretReferenceJson { get; set; }
    public bool? IsEnabled { get; set; }
    public int? TimeoutSeconds { get; set; }
}
