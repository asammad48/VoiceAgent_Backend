using VoiceAgent.Application.Dtos.ContactUs;

namespace VoiceAgent.Application.Interfaces;

public interface IContactUsService
{
    Task<Guid> CreateAsync(CreateContactUsRequestDto request, CancellationToken ct = default);
}
