namespace VoiceAgent.Infrastructure.Providers.Storage;

public class CloudflareR2RecordingStorage(CloudflareR2StorageClient client)
{
    public Task<string> SaveRecordingAsync(string key, byte[] bytes, CancellationToken ct = default)
        => client.UploadAsync(key, bytes, ct);
}
