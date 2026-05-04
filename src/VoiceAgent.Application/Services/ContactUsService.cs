using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.ContactUs;
using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

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

    public async Task<IReadOnlyList<ContactUsResponseDto>> GetAllAsync(CancellationToken ct = default)
        => await db.ContactUsMessages
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new ContactUsResponseDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                Subject = x.Subject,
                Message = x.Message,
                ResolutionStatus = x.ResolutionStatus,
                CreatedOn = x.CreatedOn,
                ResolvedOn = x.ResolvedOn
            })
            .ToListAsync(ct);

    public async Task<ContactUsResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.ContactUsMessages
            .Where(x => x.Id == id)
            .Select(x => new ContactUsResponseDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                Subject = x.Subject,
                Message = x.Message,
                ResolutionStatus = x.ResolutionStatus,
                CreatedOn = x.CreatedOn,
                ResolvedOn = x.ResolvedOn
            })
            .FirstOrDefaultAsync(ct);

    public async Task<bool> UpdateStatusAsync(Guid id, ContactUsResolutionStatus status, CancellationToken ct = default)
    {
        var entity = await db.ContactUsMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;
        }

        entity.ResolutionStatus = status;
        entity.ResolvedOn = status == ContactUsResolutionStatus.QualifiedLead ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ContactUsStatusSummaryDto> GetStatusSummaryAsync(CancellationToken ct = default)
    {
        var items = await db.ContactUsMessages
            .GroupBy(x => x.ResolutionStatus)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .ToListAsync(ct);

        return new ContactUsStatusSummaryDto
        {
            Open = items.Where(x => x.Status == ContactUsResolutionStatus.Open).Select(x => x.Count).FirstOrDefault(),
            InProgress = items.Where(x => x.Status == ContactUsResolutionStatus.InProgress).Select(x => x.Count).FirstOrDefault(),
            QualifiedLead = items.Where(x => x.Status == ContactUsResolutionStatus.QualifiedLead).Select(x => x.Count).FirstOrDefault()
        };
    }
}

