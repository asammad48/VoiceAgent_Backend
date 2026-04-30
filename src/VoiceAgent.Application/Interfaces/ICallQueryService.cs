namespace VoiceAgent.Application.Interfaces;
public interface ICallQueryService { Task<object?> GetSessionAsync(Guid callSessionId, CancellationToken ct=default); }
