using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class CampaignConfigurationService(IAppDbContext db):ICampaignConfigurationService { public async Task<Guid> CreateAsync(object request,CancellationToken ct=default){ var c=await db.Campaigns.FirstAsync(ct); var e=new CampaignConfiguration{Id=Guid.NewGuid(),TenantId=c.TenantId,ClientId=c.ClientId,CampaignId=c.Id,RequiredSlotsJson="[]",AllowedToolsJson="[]"}; db.CampaignConfigurations.Add(e); await db.SaveChangesAsync(ct); return e.Id; } }
