using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class ClientService(IAppDbContext db):IClientService { public async Task<Guid> CreateAsync(object request,CancellationToken ct=default){ var t=await db.Tenants.FirstAsync(ct); var e=new Client{Id=Guid.NewGuid(),TenantId=t.Id,Name="Client",IndustryType="general",AgentName="agent"}; db.Clients.Add(e); await db.SaveChangesAsync(ct); return e.Id; } }
