namespace VoiceAgent.Application.Interfaces;
public interface IKnowledgeBaseService { Task<Guid> CreateBaseAsync(object request, CancellationToken ct=default); }
