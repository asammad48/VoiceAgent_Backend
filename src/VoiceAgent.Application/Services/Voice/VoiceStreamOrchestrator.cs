using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Application.Interfaces.Voice;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services.Voice;

public class VoiceStreamOrchestrator(IAppDbContext db, IAudioStreamRouter audioRouter, IConversationOrchestratorService orchestrator, ISpeechEndDetectionService speechEndDetectionService, ICallCostTrackingService costTrackingService) : IVoiceStreamOrchestrator
{
    private static readonly ConcurrentDictionary<Guid, bool> BotSpeakingBySession = new();

    public async Task HandleWebSocketAsync(WebSocket socket, string streamType, CancellationToken ct = default)
    {
        var buffer = new byte[32 * 1024];
        Guid callSessionId = Guid.Empty;

        while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close) break;
            var payload = buffer.AsSpan(0, result.Count).ToArray();

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var incoming = JsonSerializer.Deserialize<VoiceEnvelope>(Encoding.UTF8.GetString(payload));
                if (incoming?.Type == "session" && Guid.TryParse(incoming.CallSessionId, out var parsed))
                {
                    callSessionId = parsed;
                    await AppendEvent(callSessionId, "voice_stream_attached", new { streamType }, ct);
                }
                continue;
            }

            if (callSessionId == Guid.Empty) continue;

            var transcript = await audioRouter.TranscribeAsync(payload, ct);
            await costTrackingService.TrackSttSecondsAsync(callSessionId, 1, ct);
            await AppendEvent(callSessionId, "stt_partial", new { transcript }, ct);

            if (!speechEndDetectionService.IsSpeechEnded(transcript)) continue;

            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = callSessionId, Speaker = "user", Text = transcript, TurnNumber = await db.CallTurns.CountAsync(x => x.CallSessionId == callSessionId, ct) + 1 });
            await db.SaveChangesAsync(ct);

            var reply = await orchestrator.OrchestrateAsync(callSessionId, transcript, ct);
            BotSpeakingBySession[callSessionId] = true;
            await costTrackingService.TrackLlmTokensAsync(callSessionId, Math.Max(1, transcript.Length / 4), Math.Max(1, reply.Length / 4), ct);

            var audio = await audioRouter.SynthesizeAsync(reply, ct);
            await costTrackingService.TrackTtsCharsAsync(callSessionId, reply.Length, ct);

            await socket.SendAsync(audio, WebSocketMessageType.Binary, true, ct);
            await AppendEvent(callSessionId, "tts_audio_sent", new { bytes = audio.Length }, ct);
            BotSpeakingBySession[callSessionId] = false;
        }

        if (socket.State == WebSocketState.Open)
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", ct);
    }

    private async Task AppendEvent(Guid callSessionId, string eventType, object payload, CancellationToken ct)
    {
        db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = callSessionId, EventType = eventType, EventDataJson = JsonSerializer.Serialize(payload) });
        await db.SaveChangesAsync(ct);
    }

    private sealed class VoiceEnvelope { public string Type { get; set; } = string.Empty; public string? CallSessionId { get; set; } }
}
