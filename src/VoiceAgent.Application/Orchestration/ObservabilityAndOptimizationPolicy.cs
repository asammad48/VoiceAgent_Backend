namespace VoiceAgent.Application.Orchestration;

public static class ObservabilityAndOptimizationPolicy
{
    public static readonly IReadOnlyCollection<string> RequiredContextIds =
    ["TenantId", "ClientId", "CampaignId", "CallSessionId", "CorrelationId"];

    public static readonly IReadOnlyCollection<string> DatabaseLogs =
    ["CallSession", "CallTurn", "CallEvent", "ToolCallLog", "ExternalSystemLog", "CallCostLog", "AuditLog"];

    public static readonly IReadOnlyCollection<string> FileLogs =
    ["DebugLogs", "SystemErrors", "ProviderFailures", "FreeSwitchTechnicalLogs", "WorkerLogs"];

    public static readonly IReadOnlyCollection<string> LlmOptimizationRules =
    [
        "DoNotSendFullMenu",
        "DoNotSendFullKnowledgeBase",
        "UseSearchToolsFirst",
        "SendTopRelevantResultsOnly",
        "KeepLast3To5Turns",
        "SummarizeOlderConversation",
        "UseStructuredExtraction",
        "UseDeterministicToolsForPricing"
    ];

    public static readonly IReadOnlyCollection<string> ElevenLabsOptimizationRules =
    [
        "KeepRepliesShort",
        "CacheCommonPhrases",
        "GenerateAudioForFinalBotMessageOnly",
        "CacheKeyByVoiceIdAndTextHash",
        "TrackCharacterUsage"
    ];

    public static readonly IReadOnlyCollection<string> DeepgramOptimizationRules =
    [
        "StreamWhileUserSpeaking",
        "EnableVadAndEndpointing",
        "StopStreamOnCallEnd",
        "AvoidSendingSilence",
        "TrackActualAudioSeconds"
    ];

    public static readonly IReadOnlyCollection<string> RagOptimizationRules =
    [
        "TopK3To5",
        "AlwaysUseMetadataFilter",
        "UseOnlyForKnowledgePolicyScriptQuestions",
        "CacheRepeatedFaqHits"
    ];
}
