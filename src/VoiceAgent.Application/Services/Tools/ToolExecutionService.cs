using VoiceAgent.Application.Interfaces.Tools;
using VoiceAgent.Application.Tools;

namespace VoiceAgent.Application.Services.Tools;

public sealed class ToolExecutionService(IEnumerable<IAgentTool> tools) : IToolExecutionService
{
    private readonly Dictionary<string, IAgentTool> _toolMap = tools.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    public async Task<ToolExecutionResult> ExecuteAsync(string toolName, ToolExecutionContext context, CancellationToken ct = default)
    {
        if (!_toolMap.TryGetValue(toolName, out var tool))
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = toolName,
                ErrorCode = "TOOL_NOT_FOUND",
                ErrorMessage = $"Tool '{toolName}' is not registered."
            };
        }

        return await tool.ExecuteAsync(context, ct);
    }
}
