namespace VoiceAgent.Application.Dtos.Branches;

public sealed class UpdateBranchRequestDto
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? DeliveryRadius { get; set; }
    public string? DeliveryRulesJson { get; set; }
    public string? BusinessHoursJson { get; set; }
    public bool? IsActive { get; set; }
}
