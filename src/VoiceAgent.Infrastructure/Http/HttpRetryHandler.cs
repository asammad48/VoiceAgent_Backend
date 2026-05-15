using System.Net;
using Microsoft.Extensions.Logging;

namespace VoiceAgent.Infrastructure.Http;

/// <summary>
/// Retries transient HTTP failures with exponential back-off + jitter.
/// Covered status codes  : 403, 404, 429, 500, 502, 503, 504
/// Covered network errors: HttpRequestException  (TCP/TLS — incl. port-443 failures)
///                         TaskCanceledException  (HTTP timeout, not user cancellation)
/// </summary>
public sealed class HttpRetryHandler(ILogger<HttpRetryHandler> logger) : DelegatingHandler
{
    private const int MaxRetries = 3;

    // 403 – transient auth/token expiry; 404 – transient routing; 429 – rate-limit;
    // 5xx – server-side transient failures.
    private static readonly HashSet<int> RetryableStatusCodes = [403, 404, 429, 500, 502, 503, 504];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Pre-read the body once so we can rebuild the content for every attempt.
        // HttpContent streams are single-read; we buffer here to make retries safe.
        byte[]? bodyBytes = null;
        List<KeyValuePair<string, IEnumerable<string>>>? contentHeaders = null;
        if (request.Content is not null)
        {
            bodyBytes      = await request.Content.ReadAsByteArrayAsync(ct);
            contentHeaders = request.Content.Headers.Select(h => h).ToList();
        }

        HttpResponseMessage? lastResponse = null;
        Exception?           lastException = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                // Exponential back-off: 1 s, 2 s, 4 s — plus up to 300 ms random jitter
                var delayMs = (int)(Math.Pow(2, attempt - 1) * 1000) + Random.Shared.Next(0, 300);

                // Respect Retry-After header when the server sends one (429 / 503)
                var retryAfterDelta = lastResponse?.Headers.RetryAfter?.Delta;
                if (retryAfterDelta.HasValue)
                    delayMs = Math.Max(delayMs, (int)retryAfterDelta.Value.TotalMilliseconds);

                logger.LogWarning(
                    "[HTTP Retry] Attempt {Attempt}/{Max} in {Delay}ms — {Method} {Uri} | last={Status}",
                    attempt, MaxRetries, delayMs,
                    request.Method, request.RequestUri,
                    lastResponse is not null ? (int)lastResponse.StatusCode : "network-error");

                await Task.Delay(delayMs, ct);
            }

            // HttpRequestMessage can only be sent once; build a fresh clone every attempt.
            using var req = BuildClone(request, bodyBytes, contentHeaders);

            try
            {
                lastResponse  = await base.SendAsync(req, ct);
                lastException = null;

                if (!RetryableStatusCodes.Contains((int)lastResponse.StatusCode))
                    return lastResponse;    // success or a non-retryable error — return as-is
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                // TCP/TLS failures, including connection refused on port 443
                lastException = ex;
                lastResponse  = null;
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested && attempt < MaxRetries)
            {
                // HTTP read/write timeout — not a user cancellation
                lastException = ex;
                lastResponse  = null;
            }
        }

        // All attempts exhausted — surface the last failure
        if (lastException is not null)
            throw lastException;

        return lastResponse!;
    }

    private static HttpRequestMessage BuildClone(
        HttpRequestMessage original,
        byte[]? bodyBytes,
        List<KeyValuePair<string, IEnumerable<string>>>? contentHeaders)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (bodyBytes is not null && contentHeaders is not null)
        {
            clone.Content = new ByteArrayContent(bodyBytes);
            foreach (var header in contentHeaders)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
