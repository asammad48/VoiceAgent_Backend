namespace VoiceAgent.Application.Interfaces;
public interface ICourierPricingService { Task<Guid> CreateProfileAsync(object request, CancellationToken ct=default); }
