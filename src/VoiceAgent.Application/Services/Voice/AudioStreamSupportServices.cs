using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces.Voice;
using VoiceAgent.Domain.Entities;
using VoiceAgent.Application.Interfaces.Providers;

namespace VoiceAgent.Application.Services.Voice;

public class AudioStreamRouter(IStreamingSpeechToTextProvider stt, IStreamingTextToSpeechProvider tts) : IAudioStreamRouter
{
    public Task<string> TranscribeAsync(byte[] audioChunk, CancellationToken ct = default) => stt.TranscribeChunkAsync(audioChunk, ct);
    public Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default) => tts.SynthesizeChunkAsync(text, ct);
}

public class SpeechEndDetectionService : ISpeechEndDetectionService
{
    public bool IsSpeechEnded(string partialOrFinalTranscript) => partialOrFinalTranscript.Trim().EndsWith('.') || partialOrFinalTranscript.Length > 24;
}

public class BargeInService : IBargeInService
{
    public bool ShouldBargeIn(string userTranscript, bool botSpeaking) => botSpeaking && !string.IsNullOrWhiteSpace(userTranscript);
}

public class CallRecordingService(IAppDbContext db) : ICallRecordingService
{
    public async Task SaveChunkAsync(Guid callSessionId, byte[] audioChunk, CancellationToken ct = default)
    {
        db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = callSessionId, EventType = "recording_chunk", EventDataJson = JsonSerializer.Serialize(new { bytes = audioChunk.Length }) });
        await db.SaveChangesAsync(ct);
    }
}

public class CallCostTrackingService(IAppDbContext db) : ICallCostTrackingService
{
    public Task TrackSttSecondsAsync(Guid callSessionId, int seconds, CancellationToken ct = default) => UpsertAsync(callSessionId, sttSec: seconds, ct: ct);
    public Task TrackTtsCharsAsync(Guid callSessionId, int chars, CancellationToken ct = default) => UpsertAsync(callSessionId, ttsChars: chars, ct: ct);
    public Task TrackLlmTokensAsync(Guid callSessionId, int inputTokens, int outputTokens, CancellationToken ct = default) => UpsertAsync(callSessionId, llmIn: inputTokens, llmOut: outputTokens, ct: ct);

    private async Task UpsertAsync(Guid callSessionId, int sttSec = 0, int ttsChars = 0, int llmIn = 0, int llmOut = 0, CancellationToken ct = default)
    {
        var session = await db.CallSessions.FirstAsync(x => x.Id == callSessionId, ct);
        var row = await db.CallCostLogs.FirstOrDefaultAsync(x => x.CallSessionId == callSessionId, ct);
        if (row is null)
        {
            row = new CallCostLog { Id = Guid.NewGuid(), CallSessionId = callSessionId, TenantId = session.TenantId, ClientId = session.ClientId, CampaignId = session.CampaignId };
            db.CallCostLogs.Add(row);
        }

        row.SttAudioSeconds += sttSec;
        row.TtsCharacters += ttsChars;
        row.LlmInputTokens += llmIn;
        row.LlmOutputTokens += llmOut;
        row.EstimatedCost = (row.SttAudioSeconds * 0.0001m) + (row.TtsCharacters * 0.00001m) + ((row.LlmInputTokens + row.LlmOutputTokens) * 0.000002m);
        await db.SaveChangesAsync(ct);
    }
}
