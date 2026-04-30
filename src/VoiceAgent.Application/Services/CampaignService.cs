using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class CampaignService(IAppDbContext db):ICampaignService { public async Task<Guid> CreateAsync(object request,CancellationToken ct=default){ var c=await db.Clients.FirstAsync(ct); var e=new Campaign{Id=Guid.NewGuid(),TenantId=c.TenantId,ClientId=c.Id,Name="Campaign"}; db.Campaigns.Add(e); await db.SaveChangesAsync(ct); return e.Id; } }
