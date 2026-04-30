namespace VoiceAgent.Application.Interfaces;
public interface ICampaignConfigurationService { Task<Guid> CreateAsync(object request, CancellationToken ct=default); }
