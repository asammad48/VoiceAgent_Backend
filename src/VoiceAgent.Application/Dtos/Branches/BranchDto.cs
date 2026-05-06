namespace VoiceAgent.Application.Dtos.Branches;

public class CreateBranchRequestDto
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? DeliveryRadius { get; set; }
    public string? DeliveryRulesJson { get; set; }
    public string? BusinessHoursJson { get; set; }
}

public class BranchResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? DeliveryRadius { get; set; }
    public string? DeliveryRulesJson { get; set; }
    public string? BusinessHoursJson { get; set; }
    public bool IsActive { get; set; }
}
