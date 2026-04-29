namespace VoiceAgent.Application.Orchestration;

public static class ConversationOrchestrationSteps
{
    public const string LoadConversationContext = "Load conversation context";
    public const string LoadCampaignConfig = "Load campaign config";
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
