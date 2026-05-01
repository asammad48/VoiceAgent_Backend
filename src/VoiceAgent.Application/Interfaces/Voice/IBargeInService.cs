namespace VoiceAgent.Application.Interfaces.Voice;
public interface IBargeInService { bool ShouldBargeIn(string userTranscript, bool botSpeaking); }
