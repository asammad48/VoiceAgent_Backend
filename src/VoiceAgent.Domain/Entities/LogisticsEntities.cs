using VoiceAgent.Common.Entities;

namespace VoiceAgent.Domain.Entities;

public class CourierPricingProfile : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal BaseFee { get; set; }
    public decimal PricePerKm { get; set; }
    public decimal PricePerKg { get; set; }
    public decimal MinimumFee { get; set; }
    public decimal? MaxDistanceKm { get; set; }
    public string? SettingsJson { get; set; }
}

public class CourierDistanceBand : AuditableEntity
{
    public Guid PricingProfileId { get; set; }
    public decimal FromKm { get; set; }
    public decimal ToKm { get; set; }
    public decimal Fee { get; set; }
}

public class CourierWeightBand : AuditableEntity
{
    public Guid PricingProfileId { get; set; }
    public decimal FromKg { get; set; }
    public decimal ToKg { get; set; }
    public decimal Fee { get; set; }
}

public class CourierZone : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ZoneDefinitionJson { get; set; } = string.Empty;
    public decimal Fee { get; set; }
}

public class CourierQuote : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string? PickupAddressJson { get; set; }
    public string? DropoffAddressJson { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal? WeightKg { get; set; }
    public string? PackageType { get; set; }
    public string? Urgency { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
    public decimal BaseFee { get; set; }
    public decimal DistanceFee { get; set; }
    public decimal WeightFee { get; set; }
    public decimal UrgencyFee { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class CabBooking : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string? PickupAddressJson { get; set; }
    public string? DropoffAddressJson { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? PassengerCount { get; set; }
    public string? VehicleType { get; set; }
    public decimal? EstimatedFare { get; set; }
    public DateTime? BookingTime { get; set; }
    public string? CustomerPhone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FinalResultJson { get; set; }
}

public class DoctorAppointment : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientPhone { get; set; }
    public bool IsExistingPatient { get; set; }
    public string? ReasonForVisit { get; set; }
    public string? PreferredDoctor { get; set; }
    public DateTime? PreferredDateTime { get; set; }
    public DateTime? AppointmentDateTime { get; set; }
    public bool EmergencyDetected { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FinalResultJson { get; set; }
}
