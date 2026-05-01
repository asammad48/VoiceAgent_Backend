namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class CabSeed
{
    public const string VehicleTypesJson = "[\"Standard\",\"Executive\",\"6-Seater\",\"Wheelchair Accessible\"]";

    public const string FareSettingsJson =
        "{\"baseFare\":3.50,\"pricePerKm\":1.80,\"minimumFare\":6.00,\"nightChargeMultiplier\":1.25,\"airportPickupFee\":5.00}";

    public const string HumanTransferJson =
        "{\"enabled\":true,\"mode\":\"OnlyOnUserRequest\",\"transferNumber\":\"+441234567890\"}";
}
