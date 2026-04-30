namespace VoiceAgent.Application.Services.Core;

public interface ISecretProvider
{
    Task<string?> GetSecretAsync(string secretKey, CancellationToken cancellationToken = default);
}

public static class ExternalDispatchStatus
{
    public const string CapturedOnly = "CapturedOnly";
    public const string SentToExternalSystem = "SentToExternalSystem";
    public const string CapturedPendingSync = "CapturedPendingSync";
}

public sealed record ExternalEndpointDefinition(
    string Method,
    string Path,
    int TimeoutSeconds);

public sealed record ExternalApiConfigurationSnapshot(
    Guid TenantId,
    Guid ClientId,
    Guid CampaignId,
    string? BaseUrl,
    string? AuthType,
    string? HeadersJson,
    string? EndpointsJson,
    string? SecretReferenceJson,
    string? RetryPolicyJson,
    bool IsEnabled);

public sealed record ExternalDispatchRequest(
    Guid TenantId,
    Guid ClientId,
    Guid CampaignId,
    Guid CallSessionId,
    string EndpointKey,
    string PayloadJson,
    string InternalEntityName,
    Guid InternalEntityId);

public sealed record ExternalDispatchResult(
    string Status,
    string UserSafeMessage,
    string? ExternalReference,
    bool RetryScheduled);

public interface IExternalApiConfigurationReader
{
    Task<ExternalApiConfigurationSnapshot?> GetAsync(Guid tenantId, Guid clientId, Guid campaignId, CancellationToken cancellationToken);
}

public interface IExternalDispatchTransport
{
    Task<ExternalTransportResponse> SendAsync(ExternalTransportRequest request, CancellationToken cancellationToken);
}

public sealed record ExternalTransportRequest(
    string BaseUrl,
    string AuthType,
    string HeadersJson,
    string SecretReferenceJson,
    string Method,
    string Path,
    int TimeoutSeconds,
    string RequestJson);

public sealed record ExternalTransportResponse(
    bool Success,
    int StatusCode,
    string ResponseJson,
    string? ExternalReference,
    string? ErrorMessage);

public interface IExternalDispatchAuditWriter
{
    Task SaveInternalCaptureAsync(ExternalDispatchRequest request, string status, CancellationToken cancellationToken);
    Task SaveToolCallLogAsync(ExternalDispatchRequest request, string status, string requestJson, string? responseJson, string? errorMessage, CancellationToken cancellationToken);
    Task SaveExternalSystemLogAsync(ExternalDispatchRequest request, string status, string? externalReference, string? responseJson, string? errorMessage, CancellationToken cancellationToken);
    Task SaveCallSessionFinalResultStatusAsync(Guid callSessionId, string status, CancellationToken cancellationToken);
}
