namespace VoiceAgent.Application.Dtos.Courier;

public sealed class UpdateCourierPricingProfileRequestDto
{
    public string? Name { get; set; }
    public string? Currency { get; set; }
    public decimal? BaseFee { get; set; }
    public decimal? PricePerKm { get; set; }
    public decimal? PricePerKg { get; set; }
    public decimal? MinimumFee { get; set; }
    public decimal? MaxDistanceKm { get; set; }
    public string? SettingsJson { get; set; }
    public bool? IsActive { get; set; }
}
