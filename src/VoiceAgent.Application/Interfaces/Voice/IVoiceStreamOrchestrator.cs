using System.Net.WebSockets;
namespace VoiceAgent.Application.Interfaces.Voice;
public interface IVoiceStreamOrchestrator { Task HandleWebSocketAsync(WebSocket socket, string streamType, CancellationToken ct = default); }
