using Microsoft.EntityFrameworkCore;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Repositories;

using VoiceAgent.Application.Features.Demo;

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

public class DemoRepository(AppDbContext db) : IDemoDataAccess
{
    public Task<CallSession?> GetSessionAsync(Guid sessionId, CancellationToken ct) => db.CallSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
    public Task<int> GetNextTurnAsync(Guid sessionId, CancellationToken ct) => db.CallTurns.CountAsync(x => x.CallSessionId == sessionId, ct).ContinueWith(x => x.Result + 1, ct);
    public Task AddSessionAsync(CallSession session, CancellationToken ct) => db.CallSessions.AddAsync(session, ct).AsTask();
    public Task AddTurnAsync(CallTurn turn, CancellationToken ct) => db.CallTurns.AddAsync(turn, ct).AsTask();
    public Task<List<MenuItem>> GetMenuItemsAsync(Guid tenantId, Guid clientId, CancellationToken ct) => db.MenuItems.Where(x => x.TenantId == tenantId && x.ClientId == clientId && x.IsAvailable).ToListAsync(ct);
    public Task<List<RestaurantDeal>> GetDealsAsync(Guid tenantId, Guid clientId, CancellationToken ct) => db.RestaurantDeals.Where(x => x.TenantId == tenantId && x.ClientId == clientId && x.IsAvailable).ToListAsync(ct);
    public Task<CourierPricingProfile?> GetCourierProfileAsync(Guid tenantId, Guid clientId, CancellationToken ct) => db.CourierPricingProfiles.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ClientId == clientId, ct);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
