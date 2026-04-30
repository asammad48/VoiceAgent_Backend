using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class CallQueryService(IAppDbContext db):ICallQueryService { public async Task<object?> GetSessionAsync(Guid callSessionId,CancellationToken ct=default)=>await db.CallSessions.FirstOrDefaultAsync(x=>x.Id==callSessionId,ct); }
