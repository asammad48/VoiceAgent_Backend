namespace VoiceAgent.Common.Responses;

public sealed class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? CorrelationId { get; set; }

    public static ApiResponse<T> Ok(T? data, string message = "Success", string? correlationId = null)
        => new() { Success = true, Message = message, Data = data, CorrelationId = correlationId };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null, string? correlationId = null)
        => new() { Success = false, Message = message, Errors = errors ?? new List<string>(), CorrelationId = correlationId };
}
