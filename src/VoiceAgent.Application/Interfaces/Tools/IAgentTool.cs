using VoiceAgent.Application.Tools;

namespace VoiceAgent.Application.Interfaces.Tools;

public interface IAgentTool
{
    string Name { get; }
    IReadOnlyCollection<string> RequiredSlots { get; }
    Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken ct = default);
}
