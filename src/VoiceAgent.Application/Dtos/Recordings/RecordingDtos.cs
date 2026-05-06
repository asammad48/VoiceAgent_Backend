namespace VoiceAgent.Application.Dtos.Recordings;
public sealed class CreateRecordingUploadUrlRequestDto { public Guid TenantId { get; set; } public Guid ClientId { get; set; } public Guid CampaignId { get; set; } public Guid CallSessionId { get; set; } public string ContentType { get; set; } = "audio/wav"; }
public sealed class RecordingUrlDto { public Guid RecordingId { get; set; } public string Url { get; set; } = string.Empty; public DateTime ExpiresAtUtc { get; set; } }
