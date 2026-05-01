namespace VoiceAgent.Application.Interfaces.Providers;
public interface IStreamingSpeechToTextProvider { Task<string> TranscribeChunkAsync(byte[] audioChunk, CancellationToken ct = default); }
