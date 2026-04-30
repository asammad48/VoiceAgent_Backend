using VoiceAgent.Application.Tools;

namespace VoiceAgent.Application.Interfaces.Tools;

public interface IToolExecutionService
{
    Task<ToolExecutionResult> ExecuteAsync(string toolName, ToolExecutionContext context, CancellationToken ct = default);
}
