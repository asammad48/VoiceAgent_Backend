using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class ProviderCostService(IAppDbContext db):IProviderCostService { public async Task<decimal> EstimateAsync(Guid callSessionId,CancellationToken ct=default){ var logs=await db.ToolCallLogs.Where(x=>x.CallSessionId==callSessionId).ToListAsync(ct); return logs.Count; } }
