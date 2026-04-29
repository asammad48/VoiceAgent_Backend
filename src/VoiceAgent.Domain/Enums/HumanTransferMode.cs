namespace VoiceAgent.Domain.Enums;

public enum HumanTransferMode
{
    Disabled = 0,
    AlwaysAllowed = 1,
    OnlyOnUserRequest = 2,
    OnlyOnFailure = 3,
    OnlyOnHighRisk = 4,
    BusinessHoursOnly = 5
}
