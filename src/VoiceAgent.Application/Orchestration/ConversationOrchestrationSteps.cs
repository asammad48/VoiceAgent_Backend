namespace VoiceAgent.Application.Orchestration;

public static class ConversationOrchestrationSteps
{
    public const string LoadConversationContext = "Load conversation context";
    public const string LoadCampaignConfig = "Load campaign config";
    public const string ValidateCampaignAndTenantActivation = "Validate campaign/tenant/client activation";
    public const string EvaluateFailurePolicy = "Evaluate campaign failure policy";
    public const string HandleUserInterruption = "Handle user interruption and intent override";
    public const string HandleHumanHandoffRequest = "Handle explicit human handoff request";
    public const string NormalizeUserInput = "Normalize user input";
    public const string DetectIntent = "Detect intent";
    public const string ExtractOrUpdateSlots = "Extract/update slots";
    public const string ValidateSlots = "Validate slots";
    public const string DetermineNextAction = "Determine next action";
    public const string ExecuteApprovedTools = "Execute approved tools";
    public const string GenerateShortResponse = "Generate short response";
    public const string PersistTurnEventsAndCost = "Persist turn/events/cost";
    public const string ReturnReplyOrAudioInstruction = "Return reply/audio instruction";
}
