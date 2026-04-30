using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class TenantService(IAppDbContext db):ITenantService { public async Task<Guid> CreateAsync(object request,CancellationToken ct=default){ var e=new Tenant{Id=Guid.NewGuid(),Name="Tenant",Slug="tenant",DefaultTimezone="UTC",DefaultCurrency="USD"}; db.Tenants.Add(e); await db.SaveChangesAsync(ct); return e.Id; } }
