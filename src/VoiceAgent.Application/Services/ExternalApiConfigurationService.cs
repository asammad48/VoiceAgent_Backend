using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class ExternalApiConfigurationService(IAppDbContext db):IExternalApiConfigurationService { public async Task<Guid> CreateAsync(object request,CancellationToken ct=default){ var c=await db.Campaigns.FirstAsync(ct); var e=new ExternalApiConfiguration{Id=Guid.NewGuid(),TenantId=c.TenantId,ClientId=c.ClientId,CampaignId=c.Id,Name="Api",BaseUrl="https://example.com",AuthType="none",HeadersJson="{}",EndpointsJson="{}",SecretReferenceJson="{}"}; db.ExternalApiConfigurations.Add(e); await db.SaveChangesAsync(ct); return e.Id; } }
