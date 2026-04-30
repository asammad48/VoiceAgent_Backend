using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class DemoConversationService(IAppDbContext db) : IDemoConversationService { public async Task<object> StartAsync(object request, CancellationToken ct=default){ var s=new CallSession{Id=Guid.NewGuid(),StartedAt=DateTime.UtcNow}; db.CallSessions.Add(s); await db.SaveChangesAsync(ct); return s.Id; } public Task<object> SendAsync(object request, CancellationToken ct=default)=>Task.FromResult<object>("ok"); }
