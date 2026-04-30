namespace VoiceAgent.Application.Services.Core;

public interface IExternalDispatchOrchestrator
{
    Task<ExternalDispatchResult> DispatchAsync(ExternalDispatchRequest request, CancellationToken cancellationToken = default);
}

public sealed class ExternalDispatchOrchestrator(
    IExternalApiConfigurationReader configurationReader,
    IExternalDispatchTransport dispatchTransport,
    IExternalDispatchAuditWriter auditWriter) : IExternalDispatchOrchestrator
{
    public async Task<ExternalDispatchResult> DispatchAsync(ExternalDispatchRequest request, CancellationToken cancellationToken = default)
    {
        var config = await configurationReader.GetAsync(request.TenantId, request.ClientId, request.CampaignId, cancellationToken);
        if (config is null || !config.IsEnabled || string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            await auditWriter.SaveInternalCaptureAsync(request, ExternalDispatchStatus.CapturedOnly, cancellationToken);
            await auditWriter.SaveCallSessionFinalResultStatusAsync(request.CallSessionId, ExternalDispatchStatus.CapturedOnly, cancellationToken);

            return new ExternalDispatchResult(
                ExternalDispatchStatus.CapturedOnly,
                "Your request is captured internally.",
                null,
                false);
        }

        // Endpoint parsing is intentionally deferred; bootstrap uses a safe default path.
        var transportRequest = new ExternalTransportRequest(
            config.BaseUrl,
            config.AuthType ?? string.Empty,
            config.HeadersJson ?? "{}",
            config.SecretReferenceJson ?? "{}",
            "POST",
            "/",
            15,
            request.PayloadJson);

        var transportResponse = await dispatchTransport.SendAsync(transportRequest, cancellationToken);
        if (transportResponse.Success)
        {
            await auditWriter.SaveToolCallLogAsync(request, ExternalDispatchStatus.SentToExternalSystem, request.PayloadJson, transportResponse.ResponseJson, null, cancellationToken);
            await auditWriter.SaveExternalSystemLogAsync(request, ExternalDispatchStatus.SentToExternalSystem, transportResponse.ExternalReference, transportResponse.ResponseJson, null, cancellationToken);
            await auditWriter.SaveCallSessionFinalResultStatusAsync(request.CallSessionId, ExternalDispatchStatus.SentToExternalSystem, cancellationToken);

            return new ExternalDispatchResult(
                ExternalDispatchStatus.SentToExternalSystem,
                "Your request has been sent.",
                transportResponse.ExternalReference,
                false);
        }

        await auditWriter.SaveInternalCaptureAsync(request, ExternalDispatchStatus.CapturedPendingSync, cancellationToken);
        await auditWriter.SaveToolCallLogAsync(request, ExternalDispatchStatus.CapturedPendingSync, request.PayloadJson, transportResponse.ResponseJson, transportResponse.ErrorMessage, cancellationToken);
        await auditWriter.SaveExternalSystemLogAsync(request, ExternalDispatchStatus.CapturedPendingSync, null, transportResponse.ResponseJson, transportResponse.ErrorMessage, cancellationToken);
        await auditWriter.SaveCallSessionFinalResultStatusAsync(request.CallSessionId, ExternalDispatchStatus.CapturedPendingSync, cancellationToken);

        return new ExternalDispatchResult(
            ExternalDispatchStatus.CapturedPendingSync,
            "Your request is captured and pending external confirmation.",
            null,
            true);
    }
}
