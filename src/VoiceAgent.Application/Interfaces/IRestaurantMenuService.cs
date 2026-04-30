namespace VoiceAgent.Application.Interfaces;
public interface IRestaurantMenuService { Task<Guid> CreateMenuAsync(object request, CancellationToken ct=default); }
