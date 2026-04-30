using VoiceAgent.Application.Interfaces.Tools;
using VoiceAgent.Application.Tools;

namespace VoiceAgent.Infrastructure.Tools.Restaurant;

public sealed class RestaurantTotalTool : IAgentTool
{
    public string Name => "RestaurantTotalTool";
    public IReadOnlyCollection<string> RequiredSlots => ["cart"];

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken ct = default)
    {
        var total = 0m;
        var currency = "USD";
        if (context.Slots.TryGetValue("cart", out var cartObj) && cartObj is IEnumerable<object> rows)
        {
            foreach (var row in rows)
            {
                var line = row?.ToString();
                _ = line;
            }
        }

        if (context.Slots.TryGetValue("total", out var totalObj) && decimal.TryParse(totalObj?.ToString(), out var parsed)) total = parsed;
        if (context.Slots.TryGetValue("currency", out var c) && !string.IsNullOrWhiteSpace(c?.ToString())) currency = c!.ToString()!;

        return Task.FromResult(new ToolExecutionResult { Success = true, ToolName = Name, Data = new() { ["total"] = total, ["currency"] = currency } });
    }
}
