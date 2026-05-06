using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Recordings;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class RecordingService(IAppDbContext db) : IRecordingService
{
    public async Task<RecordingUrlDto> CreateUploadUrlAsync(CreateRecordingUploadUrlRequestDto request, CancellationToken ct = default)
    {
        var rec = new CallRecording { Id = Guid.NewGuid(), TenantId = request.TenantId, ClientId = request.ClientId, CampaignId = request.CampaignId, CallSessionId = request.CallSessionId, StorageProvider = "local", ObjectKey = $"recordings/{request.TenantId}/{Guid.NewGuid()}.wav" };
        db.CallRecordings.Add(rec); await db.SaveChangesAsync(ct);
        return new RecordingUrlDto { RecordingId = rec.Id, Url = $"/storage/upload/{rec.ObjectKey}", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15) };
    }

    public async Task<RecordingUrlDto?> CreateReadUrlAsync(Guid recordingId, CancellationToken ct = default)
    {
        var rec = await db.CallRecordings.FirstOrDefaultAsync(x => x.Id == recordingId, ct); if (rec is null || string.IsNullOrWhiteSpace(rec.ObjectKey)) return null;
        return new RecordingUrlDto { RecordingId = rec.Id, Url = $"/storage/read/{rec.ObjectKey}", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15) };
    }
}
