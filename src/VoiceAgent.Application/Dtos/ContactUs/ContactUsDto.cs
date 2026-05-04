using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Application.Dtos.ContactUs;

public class CreateContactUsRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ContactUsResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ContactUsResolutionStatus ResolutionStatus { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class UpdateContactUsStatusRequestDto
{
    public ContactUsResolutionStatus Status { get; set; }
}

public class ContactUsStatusSummaryDto
{
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ClosedCount { get; set; }
    public int TotalCount { get; set; }
}
