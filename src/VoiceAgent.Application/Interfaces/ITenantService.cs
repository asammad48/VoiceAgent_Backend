using VoiceAgent.Application.Dtos.Tenants;

namespace VoiceAgent.Application.Interfaces;

public interface ITenantService
{
    Task<Guid> CreateAsync(CreateTenantRequestDto request, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, UpdateTenantRequestDto request, CancellationToken ct = default);
}
