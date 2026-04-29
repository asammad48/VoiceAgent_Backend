namespace VoiceAgent.Infrastructure.Persistence.Configurations;

public static class ToolCallLogConfiguration
{
    public const string Entity = "ToolCallLog";
    public static readonly string[] JsonbColumns = ["RequestJson", "ResponseJson"];
    public static readonly string[] Indexes = ["TenantId", "ClientId", "CampaignId", "CallSessionId", "CreatedOn", "Status"];
}
