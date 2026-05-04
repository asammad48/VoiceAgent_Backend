using VoiceAgent.Domain.Enums;

using VoiceAgent.Application.Dtos.ContactUs;

namespace VoiceAgent.Application.Interfaces;

public interface IContactUsService
{
    Task<Guid> CreateAsync(CreateContactUsRequestDto request, CancellationToken ct = default);
    Task<IReadOnlyList<ContactUsResponseDto>> GetAllAsync(CancellationToken ct = default);
    Task<ContactUsResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(Guid id, ContactUsResolutionStatus status, CancellationToken ct = default);
    Task<ContactUsStatusSummaryDto> GetStatusSummaryAsync(CancellationToken ct = default);
}
