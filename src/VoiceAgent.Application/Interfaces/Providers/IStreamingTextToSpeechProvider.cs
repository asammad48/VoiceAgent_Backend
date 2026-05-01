namespace VoiceAgent.Application.Interfaces.Providers;
public interface IStreamingTextToSpeechProvider { Task<byte[]> SynthesizeChunkAsync(string text, CancellationToken ct = default); }
