using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class CourierPricingService(IAppDbContext db):ICourierPricingService { public async Task<Guid> CreateProfileAsync(object request,CancellationToken ct=default){ var c=await db.Clients.FirstAsync(ct); var e=new CourierPricingProfile{Id=Guid.NewGuid(),TenantId=c.TenantId,ClientId=c.Id,Name="Std",Currency="USD"}; db.CourierPricingProfiles.Add(e); await db.SaveChangesAsync(ct); return e.Id; } }
