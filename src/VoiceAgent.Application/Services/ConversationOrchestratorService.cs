using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class ConversationOrchestratorService : IConversationOrchestratorService { public Task<string> OrchestrateAsync(Guid callSessionId,string message,CancellationToken ct=default)=>Task.FromResult("orchestrated"); }
