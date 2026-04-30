namespace VoiceAgent.Domain.Enums;
public enum ConversationState { Started, Greeting, ConsentDisclosure, IntentDetection, CollectingSlots, ValidatingSlots, ExecutingTool, ConfirmingAction, SavingResult, ExternalDispatch, HumanHandoff, Completed, Failed, Abandoned }

