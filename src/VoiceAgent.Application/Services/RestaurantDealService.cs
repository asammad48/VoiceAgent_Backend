using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class RestaurantDealService(IAppDbContext db):IRestaurantDealService { public async Task<Guid> CreateDealAsync(object request,CancellationToken ct=default){ var c=await db.Clients.FirstAsync(ct); var e=new RestaurantDeal{Id=Guid.NewGuid(),TenantId=c.TenantId,ClientId=c.Id,Name="Deal",Description="",DealPrice=1,Currency="USD",AvailabilityScheduleJson="{}",MetadataJson="{}"}; db.RestaurantDeals.Add(e); await db.SaveChangesAsync(ct); return e.Id; } }
