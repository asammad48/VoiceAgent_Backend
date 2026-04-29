namespace VoiceAgent.Infrastructure.Caching;

public static class TtsCachePathBuilder
{
    public static string BuildCachedPhrasePath(Guid tenantId, Guid clientId, string voiceId, string phraseHash)
        => $"storage/tts-cache/{tenantId}/{clientId}/{voiceId}/{phraseHash}.mp3";

    public static string BuildTempChunkDirectory(Guid callSessionId)
        => $"/tmp/voiceagent/{callSessionId}/chunks/";

    public static string BuildRecordingObjectKey(Guid tenantId, Guid clientId, Guid campaignId, Guid callSessionId)
        => $"recordings/{tenantId}/{clientId}/{campaignId}/{callSessionId}.mp3";
}
