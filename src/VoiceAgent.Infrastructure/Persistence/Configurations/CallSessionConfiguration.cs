namespace VoiceAgent.Infrastructure.Persistence.Configurations;

public static class CallSessionConfiguration
{
    public const string Entity = "CallSession";
    public static readonly string[] JsonbColumns = ["CollectedSlotsJson", "FinalResultJson", "SummaryJson"];
    public static readonly string[] Indexes = ["TenantId", "ClientId", "CampaignId", "CreatedOn", "Status", "IsActive", "IsDeleted"];
}
