namespace VoiceAgent.Domain.Enums;
public enum ConversationState
{
    Started, Greeting, ConsentDisclosure, IntentDetection,
    CollectingSlots, ValidatingSlots, ExecutingTool, ConfirmingAction,
    AwaitingConfirmation,   // all slots filled — summary read, waiting for yes/no
    Closing,               // user confirmed — delivering goodbye
    SavingResult, ExternalDispatch, HumanHandoff,
    Completed,             // happy-path close
    Declined,              // user not interested
    Disqualified,          // user does not qualify
    AbuseEnded,            // call ended due to abuse
    Failed, Abandoned
}

