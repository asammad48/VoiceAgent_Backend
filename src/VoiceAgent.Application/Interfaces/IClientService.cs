namespace VoiceAgent.Application.Interfaces;
public interface IClientService { Task<Guid> CreateAsync(object request, CancellationToken ct=default); }
