namespace VoiceAgent.Common.Providers;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
