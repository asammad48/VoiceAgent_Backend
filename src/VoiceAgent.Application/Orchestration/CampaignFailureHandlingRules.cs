namespace VoiceAgent.Application.Orchestration;

public static class CampaignFailureHandlingRules
{
    public const string ConversationPriority = "Listen -> Understand intent -> Apply campaign config -> Collect missing data -> Execute deterministic tools -> Generate short response -> Log all";
    public const string BotOnlyFailureFlow = "When handoff is disabled: Failure -> Save partial result -> Safe closure -> FailedNoHandoff/CapturedOnly";
    public const string HumanTransferFlow = "When handoff is enabled and trigger matches: AnyState -> HumanHandoff -> Transfer (FreeSWITCH when available)";
    public const string LlmFailure = "Fail once -> retry once; fail twice -> fallback response; if handoff enabled transfer; if disabled save partial and close";
    public const string SttFailure = "Ask user to repeat; on repeated failure use fallback/handoff/close from campaign config";
    public const string TtsFailure = "Web demo returns text; phone retries TTS once then fallback cached audio when available";
    public const string ExternalApiFailure = "Save as CapturedPendingSync, log request/response/error, worker retries later, never claim external success";
    public const string NoExternalApiConfigured = "Save final result internally as JSONB with status CapturedOnly and confirm request captured";
    public const string MenuItemNotFound = "Search closest items, offer up to 3 alternatives, never add unknown item";
    public const string DealNotFound = "List available deals/categories and ask whether user wants menu instead";
    public const string AddressUnclear = "Ask to repeat address/postcode; do not calculate distance before geocoding succeeds";
    public const string OutsideDeliveryRadius = "Inform politely, offer pickup when supported, do not accept delivery order";
    public const string PriceCannotBeCalculated = "Never invent price; ask missing data or save for manual review based on campaign config";
    public const string UserSilent = "Prompt once, prompt twice, then close or transfer per campaign config";
    public const string UserInterrupts = "Stop bot speech, listen immediately, update state with new intent";
    public const string UserRequestsHuman = "If handoff enabled transfer; if disabled explain limitation and save request when possible";
    public const string CampaignConfigMissing = "Reject start request, return setup error, log configuration failure";
    public const string TenantOrClientInactive = "Reject request";
    public const string CallDrops = "Mark Abandoned and save partial slots/transcript";
}
