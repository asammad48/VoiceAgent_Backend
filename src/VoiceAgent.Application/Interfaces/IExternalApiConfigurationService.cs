namespace VoiceAgent.Application.Interfaces;
public interface IExternalApiConfigurationService { Task<Guid> CreateAsync(object request, CancellationToken ct=default); }
