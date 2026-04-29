namespace VoiceAgent.Application.Orchestration;

public static class VoicePipelinePolicy
{
    public static readonly IReadOnlyCollection<string> EntryPoints =
    [
        "WebsiteTextDemo",
        "WebsiteVoiceDemo",
        "SipClientTesting",
        "FreeSwitchPhoneFlow",
        "TelnyxProviderFlow"
    ];

    public const string ElevenLabsTriggerRule = "Call ElevenLabs only after orchestrator generates final user-facing reply for the turn";

    public static readonly IReadOnlyCollection<string> ElevenLabsDoNotCallFor =
    [
        "InternalReasoning",
        "ToolExecutionMessages",
        "DraftResponses",
        "HiddenPrompts",
        "RagContext"
    ];

    public static readonly IReadOnlyCollection<string> ElevenLabsCallFor =
    [
        "GreetingAudio",
        "ClarificationQuestion",
        "FinalAnswer",
        "ConfirmationPrompt",
        "CompletionMessage",
        "FallbackMessage"
    ];

    public static readonly IReadOnlyCollection<string> CommonCachedPhrases =
    [
        "Hi, how can I help you today?",
        "Would you like anything else?",
        "Is that for delivery or pickup?",
        "Can you repeat that, please?",
        "Your request has been captured."
    ];
}
