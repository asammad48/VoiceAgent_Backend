using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Application.Interfaces.Voice;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services.Voice;

public class VoiceStreamOrchestrator(
    IAppDbContext db,
    IAudioStreamRouter audioRouter,
    IConversationOrchestratorService orchestrator,
    ICallCostTrackingService costTrackingService,
    ILogger<VoiceStreamOrchestrator> logger) : IVoiceStreamOrchestrator
{
    // ── Per-session state ─────────────────────────────────────────────────────

    private static readonly ConcurrentDictionary<Guid, bool> BotSpeaking = new();
    private static readonly ConcurrentDictionary<Guid, AudioAccumulator> AudioAccumulators = new();

    // Energy threshold (RMS out of 32767): below this is treated as silence.
    // Typical speech RMS at normal mic gain is 800–4000; background noise is ~100–300.
    private const double SpeechEnergyThreshold = 400.0;

    // How long silence must persist after speech before we transcribe.
    private const double SilenceThresholdSeconds = 1.5;

    // WAV header is always 44 bytes; PCM samples follow as little-endian Int16.
    private const int WavHeaderBytes = 44;
    private const int SampleRate = 16000;

    // ── Main entry point ──────────────────────────────────────────────────────

    public async Task HandleWebSocketAsync(WebSocket socket, string streamType, CancellationToken ct = default)
    {
        var buffer        = new byte[64 * 1024];
        var callSessionId = Guid.Empty;

        logger.LogInformation("[WS:{StreamType}] WebSocket connected", streamType);

        try
        {
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("[WS:{StreamType}] Session={Id} Client sent close frame", streamType, callSessionId);
                    break;
                }

                var payload = buffer.AsSpan(0, result.Count).ToArray();

                // ── Text frame: session-attach ────────────────────────────────
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text     = Encoding.UTF8.GetString(payload);
                    var envelope = TryDeserialize<VoiceEnvelope>(text);

                    if (envelope?.Type == "session" && Guid.TryParse(envelope.CallSessionId, out var parsed))
                    {
                        callSessionId = parsed;
                        logger.LogInformation("[WS:{StreamType}] Session={Id} Session attached", streamType, callSessionId);
                        await AppendEventAsync(callSessionId, "voice_stream_attached", new { streamType }, ct);
                        await SendOpeningScriptAsync(socket, callSessionId, streamType, ct);
                    }
                    else
                    {
                        logger.LogWarning("[WS:{StreamType}] Unrecognised text frame: {Text}", streamType, text);
                    }
                    continue;
                }

                // ── Binary frame: audio chunk ─────────────────────────────────
                if (callSessionId == Guid.Empty)
                {
                    logger.LogWarning("[WS:{StreamType}] Binary before session attach — dropping {Bytes} bytes", streamType, payload.Length);
                    continue;
                }

                // Discard audio arriving while the bot is speaking (mic-echo suppression)
                if (BotSpeaking.TryGetValue(callSessionId, out var botIsOn) && botIsOn)
                {
                    logger.LogDebug("[WS:{StreamType}] Session={Id} Discarding {Bytes} bytes (bot speaking)", streamType, callSessionId, payload.Length);
                    continue;
                }

                // ── Energy-based VAD ──────────────────────────────────────────
                var rms      = ComputeRms(payload);
                var isSpeech = rms > SpeechEnergyThreshold;

                logger.LogDebug("[WS:{StreamType}] Session={Id} Chunk {Bytes}b RMS={Rms:0} speech={Speech}",
                    streamType, callSessionId, payload.Length, rms, isSpeech);

                var acc = AudioAccumulators.GetOrAdd(callSessionId, _ => new AudioAccumulator());

                if (isSpeech)
                {
                    // Accumulate raw PCM (strip the 44-byte WAV header)
                    var pcm = payload.Length > WavHeaderBytes
                        ? payload.AsSpan(WavHeaderBytes).ToArray()
                        : Array.Empty<byte>();
                    acc.PcmBuffer.AddRange(pcm);
                    acc.LastSpeechAt = DateTime.UtcNow;
                    acc.HasSpeech    = true;
                    continue;
                }

                // Silent chunk — if no speech accumulated yet, nothing to do
                if (!acc.HasSpeech) continue;

                // Include silence in buffer (natural speech has brief mid-word pauses)
                var silencePcm = payload.Length > WavHeaderBytes
                    ? payload.AsSpan(WavHeaderBytes).ToArray()
                    : Array.Empty<byte>();
                acc.PcmBuffer.AddRange(silencePcm);

                var silenceElapsed = (DateTime.UtcNow - acc.LastSpeechAt).TotalSeconds;
                if (silenceElapsed < SilenceThresholdSeconds) continue;

                // ── Speech turn complete ──────────────────────────────────────
                var pcmData = acc.PcmBuffer.ToArray();
                AudioAccumulators[callSessionId] = new AudioAccumulator();   // reset

                var durationSec = pcmData.Length / (SampleRate * 2.0);
                logger.LogInformation("[WS:{StreamType}] Session={Id} Speech end — {Dur:0.1}s accumulated, transcribing",
                    streamType, callSessionId, durationSec);

                // Wrap accumulated PCM in a single WAV and transcribe
                var fullWav    = BuildWav(pcmData);
                var transcript = await audioRouter.TranscribeAsync(fullWav, ct);
                await costTrackingService.TrackSttSecondsAsync(callSessionId, (int)Math.Ceiling(durationSec), ct);

                logger.LogInformation("[WS:{StreamType}] Session={Id} STT: \"{Transcript}\"", streamType, callSessionId, transcript);
                await AppendEventAsync(callSessionId, "stt_final", new { transcript, durationSec }, ct);

                if (string.IsNullOrWhiteSpace(transcript))
                {
                    logger.LogDebug("[WS:{StreamType}] Session={Id} Empty transcript — discarding", streamType, callSessionId);
                    continue;
                }

                var reply = await orchestrator.OrchestrateAsync(callSessionId, transcript, ct);
                logger.LogInformation("[WS:{StreamType}] Session={Id} LLM reply: \"{Reply}\"", streamType, callSessionId, reply);
                await costTrackingService.TrackLlmTokensAsync(
                    callSessionId,
                    Math.Max(1, transcript.Length / 4),
                    Math.Max(1, reply.Length / 4), ct);

                await SendBotTurnAsync(socket, callSessionId, reply, streamType, ct);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[WS:{StreamType}] Session={Id} WebSocket cancelled", streamType, callSessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[WS:{StreamType}] Session={Id} Unhandled error", streamType, callSessionId);
        }
        finally
        {
            BotSpeaking.TryRemove(callSessionId, out _);
            AudioAccumulators.TryRemove(callSessionId, out _);

            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", ct);
                logger.LogInformation("[WS:{StreamType}] Session={Id} WebSocket closed normally", streamType, callSessionId);
            }
            else
            {
                logger.LogInformation("[WS:{StreamType}] Session={Id} WebSocket disconnected (state={State})", streamType, callSessionId, socket.State);
            }
        }
    }

    // ── Bot speaks first ──────────────────────────────────────────────────────

    private async Task SendOpeningScriptAsync(WebSocket socket, Guid callSessionId, string streamType, CancellationToken ct)
    {
        try
        {
            var session = await db.CallSessions.FirstOrDefaultAsync(s => s.Id == callSessionId, ct);
            if (session is null) return;

            var config = await db.CampaignConfigurations
                .FirstOrDefaultAsync(x => x.CampaignId == session.CampaignId && x.TenantId == session.TenantId && x.IsActive, ct);

            if (config is null || string.IsNullOrWhiteSpace(config.QuestionnaireJson)) return;

            var questionnaire = TryDeserialize<QuestionnaireDefinition>(config.QuestionnaireJson);
            if (string.IsNullOrWhiteSpace(questionnaire?.OpeningScript)) return;

            logger.LogInformation("[WS:{StreamType}] Session={Id} Sending opening script", streamType, callSessionId);
            await SendBotTurnAsync(socket, callSessionId, questionnaire.OpeningScript, streamType, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[WS:{StreamType}] Session={Id} Failed to send opening script", streamType, callSessionId);
        }
    }

    // ── Synthesise TTS and send with control frames ───────────────────────────

    private async Task SendBotTurnAsync(WebSocket socket, Guid callSessionId, string text, string streamType, CancellationToken ct)
    {
        BotSpeaking[callSessionId] = true;
        // Discard any audio accumulated during bot speech
        AudioAccumulators[callSessionId] = new AudioAccumulator();

        try
        {
            await SendControlFrameAsync(socket, "bot_started", ct);

            logger.LogInformation("[WS:{StreamType}] Session={Id} Synthesising TTS ({Chars} chars)", streamType, callSessionId, text.Length);
            var audio = await audioRouter.SynthesizeAsync(text, ct);
            await costTrackingService.TrackTtsCharsAsync(callSessionId, text.Length, ct);

            logger.LogInformation("[WS:{StreamType}] Session={Id} Sending TTS audio ({Bytes} bytes)", streamType, callSessionId, audio.Length);
            await socket.SendAsync(audio, WebSocketMessageType.Binary, true, ct);
            await AppendEventAsync(callSessionId, "tts_audio_sent", new { bytes = audio.Length }, ct);
        }
        finally
        {
            BotSpeaking[callSessionId] = false;
            await SendControlFrameAsync(socket, "bot_ended", ct);
        }
    }

    // ── Control frame ─────────────────────────────────────────────────────────

    private static async Task SendControlFrameAsync(WebSocket socket, string type, CancellationToken ct)
    {
        if (socket.State != WebSocketState.Open) return;
        var frame = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type }));
        await socket.SendAsync(frame, WebSocketMessageType.Text, true, ct);
    }

    // ── Event logging ─────────────────────────────────────────────────────────

    private async Task AppendEventAsync(Guid callSessionId, string eventType, object payload, CancellationToken ct)
    {
        db.CallEvents.Add(new CallEvent
        {
            Id = Guid.NewGuid(), CallSessionId = callSessionId,
            EventType = eventType, EventDataJson = JsonSerializer.Serialize(payload)
        });
        await db.SaveChangesAsync(ct);
    }

    // ── Energy-based VAD ──────────────────────────────────────────────────────

    /// <summary>Computes RMS of the PCM samples inside a WAV chunk.</summary>
    private static double ComputeRms(byte[] wavChunk)
    {
        if (wavChunk.Length <= WavHeaderBytes) return 0;
        int sampleCount = (wavChunk.Length - WavHeaderBytes) / 2;
        if (sampleCount == 0) return 0;
        double sum = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            short s = BitConverter.ToInt16(wavChunk, WavHeaderBytes + i * 2);
            sum += (double)s * s;
        }
        return Math.Sqrt(sum / sampleCount);
    }

    /// <summary>Wraps raw PCM bytes in a minimal mono 16-bit WAV header.</summary>
    private static byte[] BuildWav(byte[] pcmData)
    {
        const int channels      = 1;
        const int bitsPerSample = 16;
        int byteRate   = SampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;

        var buf = new byte[WavHeaderBytes + pcmData.Length];

        void Str(int o, string s) { for (int i = 0; i < s.Length; i++) buf[o + i] = (byte)s[i]; }
        void I32(int o, int v)    { BitConverter.TryWriteBytes(buf.AsSpan(o), v); }
        void I16(int o, short v)  { BitConverter.TryWriteBytes(buf.AsSpan(o), v); }

        Str(0,  "RIFF"); I32(4, 36 + pcmData.Length); Str(8, "WAVE");
        Str(12, "fmt "); I32(16, 16);
        I16(20, 1);                         // PCM
        I16(22, channels);
        I32(24, SampleRate);
        I32(28, byteRate);
        I16(32, (short)blockAlign);
        I16(34, bitsPerSample);
        Str(36, "data"); I32(40, pcmData.Length);
        Array.Copy(pcmData, 0, buf, WavHeaderBytes, pcmData.Length);

        return buf;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static T? TryDeserialize<T>(string json) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(json, JsonOpts); }
        catch { return null; }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // ── Inner types ───────────────────────────────────────────────────────────

    private sealed class VoiceEnvelope
    {
        public string Type { get; set; } = string.Empty;
        public string? CallSessionId { get; set; }
    }

    private sealed class QuestionnaireDefinition
    {
        [JsonPropertyName("openingScript")] public string? OpeningScript { get; set; }
    }

    private sealed class AudioAccumulator
    {
        public List<byte> PcmBuffer { get; } = new(SampleRate * 2 * 30); // pre-alloc ~30s
        public DateTime LastSpeechAt { get; set; } = DateTime.MinValue;
        public bool HasSpeech { get; set; }
    }
}
