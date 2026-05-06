namespace VoiceAgent.Application.Dtos.ExternalApis;

public class CreateExternalApiConfigurationRequestDto
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthType { get; set; } = string.Empty;
    public string? HeadersJson { get; set; }
    public string? EndpointsJson { get; set; }
    public string? SecretReferenceJson { get; set; }
    public bool IsEnabled { get; set; }
    public int? TimeoutSeconds { get; set; }
}

public class ExternalApiConfigurationResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthType { get; set; } = string.Empty;
    public string? HeadersJson { get; set; }
    public string? EndpointsJson { get; set; }
    public string? SecretReferenceJson { get; set; }
    public bool IsEnabled { get; set; }
    public int? TimeoutSeconds { get; set; }
}
