using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Features.Demo;

public interface IDemoDataAccess
{
    Task<CallSession?> GetSessionAsync(Guid sessionId, CancellationToken ct);
    Task<int> GetNextTurnAsync(Guid sessionId, CancellationToken ct);
    Task AddSessionAsync(CallSession session, CancellationToken ct);
    Task AddTurnAsync(CallTurn turn, CancellationToken ct);
    Task<List<MenuItem>> GetMenuItemsAsync(Guid tenantId, Guid clientId, CancellationToken ct);
    Task<List<RestaurantDeal>> GetDealsAsync(Guid tenantId, Guid clientId, CancellationToken ct);
    Task<CourierPricingProfile?> GetCourierProfileAsync(Guid tenantId, Guid clientId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
