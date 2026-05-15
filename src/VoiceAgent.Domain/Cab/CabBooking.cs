namespace VoiceAgent.Domain.Entities;

public class CabBooking
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CallSessionId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = string.Empty;
    public string DropoffLocation { get; set; } = string.Empty;
    public string PickupDateTime { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EstimatedFare { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsAirportPickup { get; set; }
    public bool IsNightSurcharge { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
