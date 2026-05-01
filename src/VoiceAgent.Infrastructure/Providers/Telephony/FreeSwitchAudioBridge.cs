namespace VoiceAgent.Infrastructure.Providers.Telephony;

public class FreeSwitchAudioBridge
{
    public Task<string> SendAudioAsync(Guid callSessionId, byte[] audio, CancellationToken ct = default)
        => Task.FromResult($"freeswitch-audio:{callSessionId}:{audio.Length}");
}
