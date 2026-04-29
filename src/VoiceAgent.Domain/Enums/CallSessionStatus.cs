namespace VoiceAgent.Domain.Enums;

public enum CallSessionStatus
{
    Started,
    InProgress,
    Completed,
    Failed,
    TransferredToHuman,
    Abandoned,
    CapturedOnly,
    CapturedPendingSync
}
