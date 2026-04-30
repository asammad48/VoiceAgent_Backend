using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class RestaurantMenuService(IAppDbContext db):IRestaurantMenuService { public async Task<Guid> CreateMenuAsync(object request,CancellationToken ct=default){ var c=await db.Clients.FirstAsync(ct); var e=new RestaurantMenu{Id=Guid.NewGuid(),TenantId=c.TenantId,ClientId=c.Id,Name="Menu"}; db.RestaurantMenus.Add(e); await db.SaveChangesAsync(ct); return e.Id; } }
