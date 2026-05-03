using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.ContactUs;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;

public class ContactUsService(IAppDbContext db) : IContactUsService
{
    public async Task<Guid> CreateAsync(CreateContactUsRequestDto request, CancellationToken ct = default)
    {
        var entity = new ContactUs
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Subject = request.Subject,
            Message = request.Message,
            CreatedOn = DateTime.UtcNow
        };

        db.ContactUsMessages.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
