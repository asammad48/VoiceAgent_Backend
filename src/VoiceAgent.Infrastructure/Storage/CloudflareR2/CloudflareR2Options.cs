namespace VoiceAgent.Infrastructure.Storage.CloudflareR2;

public class CloudflareR2Options
{
    public const string SectionName = "CloudflareR2";
    public const string RecordingStorageProviderName = "CloudflareR2";

    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
}
