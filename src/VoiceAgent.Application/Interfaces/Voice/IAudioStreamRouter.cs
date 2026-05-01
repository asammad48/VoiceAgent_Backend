namespace VoiceAgent.Application.Interfaces.Voice;
public interface IAudioStreamRouter { Task<string> TranscribeAsync(byte[] audioChunk, CancellationToken ct = default); Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default); }
