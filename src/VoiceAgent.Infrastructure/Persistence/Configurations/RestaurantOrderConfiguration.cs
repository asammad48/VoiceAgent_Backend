namespace VoiceAgent.Infrastructure.Persistence.Configurations;

public static class RestaurantOrderConfiguration
{
    public const string Entity = "RestaurantOrder";
    public static readonly string[] JsonbColumns = ["ItemsJson", "DealsJson", "AddressJson"];
    public static readonly string[] Indexes = ["TenantId", "ClientId", "CampaignId", "CallSessionId", "CreatedOn", "Status"];
}
