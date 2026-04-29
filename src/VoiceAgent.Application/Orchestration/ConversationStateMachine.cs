namespace VoiceAgent.Application.Orchestration;

public enum ConversationState
{
    Started = 0,
    Greeting = 1,
    ConsentDisclosure = 2,
    IntentDetection = 3,
    CollectingSlots = 4,
    ValidatingSlots = 5,
    ExecutingTool = 6,
    ConfirmingAction = 7,
    SavingResult = 8,
    ExternalDispatch = 9,
    HumanHandoff = 10,
    Completed = 11,
    Failed = 12,
    Abandoned = 13
}

public static class ConversationStateTransitions
{
    public static IReadOnlyCollection<ConversationState> BuildMainFlow(bool requiresConsentDisclosure, bool hasExternalApi)
    {
        var flow = new List<ConversationState>
        {
            ConversationState.Started,
            ConversationState.Greeting
        };

        if (requiresConsentDisclosure)
        {
            flow.Add(ConversationState.ConsentDisclosure);
        }

        flow.AddRange(
        [
            ConversationState.IntentDetection,
            ConversationState.CollectingSlots,
            ConversationState.ValidatingSlots,
            ConversationState.ExecutingTool,
            ConversationState.ConfirmingAction,
            ConversationState.SavingResult
        ]);

        if (hasExternalApi)
        {
            flow.Add(ConversationState.ExternalDispatch);
        }

        flow.Add(ConversationState.Completed);
        return flow;
    }

    public static bool CanMoveToHumanHandoff(bool handoffEnabled, bool triggerMatched)
        => handoffEnabled && triggerMatched;

    public static ConversationState ResolveFailureTerminalState(bool handoffEnabled)
        => ConversationState.Failed;
}
