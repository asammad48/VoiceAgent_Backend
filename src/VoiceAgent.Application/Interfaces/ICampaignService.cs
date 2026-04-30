namespace VoiceAgent.Application.Interfaces;
public interface ICampaignService { Task<Guid> CreateAsync(object request, CancellationToken ct=default); }
