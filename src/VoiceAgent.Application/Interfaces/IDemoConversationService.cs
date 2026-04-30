namespace VoiceAgent.Application.Interfaces;
public interface IDemoConversationService { Task<object> StartAsync(object request, CancellationToken ct=default); Task<object> SendAsync(object request, CancellationToken ct=default); }
