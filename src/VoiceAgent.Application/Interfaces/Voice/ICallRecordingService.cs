namespace VoiceAgent.Application.Interfaces.Voice;
public interface ICallRecordingService { Task SaveChunkAsync(Guid callSessionId, byte[] audioChunk, CancellationToken ct = default); }
