using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Domain.Entities;

public class ContactUs
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ContactUsResolutionStatus ResolutionStatus { get; set; } = ContactUsResolutionStatus.Open;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
