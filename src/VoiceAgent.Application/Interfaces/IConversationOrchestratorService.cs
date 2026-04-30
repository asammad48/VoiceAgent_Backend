namespace VoiceAgent.Application.Interfaces;
public interface IConversationOrchestratorService { Task<string> OrchestrateAsync(Guid callSessionId, string message, CancellationToken ct=default); }
