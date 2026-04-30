namespace VoiceAgent.Application.Interfaces;
public interface IRestaurantDealService { Task<Guid> CreateDealAsync(object request, CancellationToken ct=default); }
