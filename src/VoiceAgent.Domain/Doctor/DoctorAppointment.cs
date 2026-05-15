namespace VoiceAgent.Domain.Entities;

public class DoctorAppointment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ReasonForVisit { get; set; } = string.Empty;
    public string PreferredDateTime { get; set; } = string.Empty;
    public string PreferredDoctor { get; set; } = string.Empty;
    public string ClinicBranch { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
