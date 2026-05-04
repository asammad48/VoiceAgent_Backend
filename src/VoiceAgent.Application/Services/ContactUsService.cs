using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.ContactUs;
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
            ResolutionStatus = ContactUsResolutionStatus.Open,
            CreatedOn = DateTime.UtcNow
        };

        db.ContactUsMessages.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<IReadOnlyList<ContactUsResponseDto>> GetAllAsync(CancellationToken ct = default)
        => await db.ContactUsMessages
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedOn)
            .Select(MapToDto())
            .ToListAsync(ct);

    public async Task<ContactUsResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.ContactUsMessages
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(MapToDto())
            .FirstOrDefaultAsync(ct);

    public async Task<bool> UpdateStatusAsync(Guid id, ContactUsResolutionStatus status, CancellationToken ct = default)
    {
        var entity = await db.ContactUsMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;
        }

        entity.ResolutionStatus = status;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ContactUsStatusSummaryDto> GetStatusSummaryAsync(CancellationToken ct = default)
    {
        var grouped = await db.ContactUsMessages
            .AsNoTracking()
            .GroupBy(x => x.ResolutionStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new ContactUsStatusSummaryDto
        {
            OpenCount = grouped.FirstOrDefault(x => x.Status == ContactUsResolutionStatus.Open)?.Count ?? 0,
            InProgressCount = grouped.FirstOrDefault(x => x.Status == ContactUsResolutionStatus.InProgress)?.Count ?? 0,
            ResolvedCount = grouped.FirstOrDefault(x => x.Status == ContactUsResolutionStatus.Resolved)?.Count ?? 0,
            ClosedCount = grouped.FirstOrDefault(x => x.Status == ContactUsResolutionStatus.Closed)?.Count ?? 0,
            TotalCount = grouped.Sum(x => x.Count)
        };
    }

    private static System.Linq.Expressions.Expression<Func<ContactUs, ContactUsResponseDto>> MapToDto()
        => entity => new ContactUsResponseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            Subject = entity.Subject,
            Message = entity.Message,
            ResolutionStatus = entity.ResolutionStatus,
            CreatedOn = entity.CreatedOn
        };
}
