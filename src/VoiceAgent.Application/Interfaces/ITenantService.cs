namespace VoiceAgent.Application.Interfaces;
public interface ITenantService { Task<Guid> CreateAsync(object request, CancellationToken ct=default); }
