using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class KnowledgeBaseService(IAppDbContext db):IKnowledgeBaseService { public async Task<Guid> CreateBaseAsync(object request,CancellationToken ct=default){ var c=await db.Campaigns.FirstAsync(ct); var e=new KnowledgeBase{Id=Guid.NewGuid(),TenantId=c.TenantId,ClientId=c.ClientId,CampaignId=c.Id,Name="KB",Description=""}; db.KnowledgeBases.Add(e); await db.SaveChangesAsync(ct); return e.Id; } }
