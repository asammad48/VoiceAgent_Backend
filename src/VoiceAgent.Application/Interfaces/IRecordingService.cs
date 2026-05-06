using VoiceAgent.Application.Dtos.Recordings;

namespace VoiceAgent.Application.Interfaces;
public interface IRecordingService
{
    Task<RecordingUrlDto> CreateUploadUrlAsync(CreateRecordingUploadUrlRequestDto request, CancellationToken ct = default);
    Task<RecordingUrlDto?> CreateReadUrlAsync(Guid recordingId, CancellationToken ct = default);
}
