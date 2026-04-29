namespace VoiceAgent.Application.Services.Core;

public interface IVoiceSessionService;
public interface IVoiceStreamingService;

public interface ITtsPolicyService
{
    bool ShouldGenerateAudio(string userFacingReply, bool isInternalMessage);
}

public interface ISttPolicyService
{
    bool ShouldAcceptTranscript(bool isFinalTranscript, bool botIsSpeaking, bool bargeInEnabled);
}
