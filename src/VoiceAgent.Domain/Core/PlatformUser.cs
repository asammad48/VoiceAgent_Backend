using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Domain.Entities;

public class PlatformUser
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ClientId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserPlatformRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
