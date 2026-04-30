namespace VoiceAgent.WorkerService.Contracts;

public static class WorkerServiceRuntimeNotes
{
    public const string OutboundDialRule = "Outbound dialing loop must run in WorkerService, never in live WebAPI path.";
    public const string LiveConversationRule = "Conversation execution can continue after answer, but dialing orchestration remains worker-owned.";
}
