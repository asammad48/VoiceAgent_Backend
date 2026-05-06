using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Tenants;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;

public class TenantService(IAppDbContext db) : ITenantService
{
    public async Task<Guid> CreateAsync(CreateTenantRequestDto request, CancellationToken ct = default)
    {
        var e = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug,
            DefaultTimezone = request.DefaultTimezone,
            DefaultCurrency = request.DefaultCurrency,
            IsActive = true
        };

        db.Tenants.Add(e);
        await db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateTenantRequestDto request, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tenant is null)
        {
            return false;
        }

        tenant.Name = request.Name;
        tenant.Slug = request.Slug;
        tenant.DefaultTimezone = request.DefaultTimezone;
        tenant.DefaultCurrency = request.DefaultCurrency;
        tenant.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        return true;
    }
}
