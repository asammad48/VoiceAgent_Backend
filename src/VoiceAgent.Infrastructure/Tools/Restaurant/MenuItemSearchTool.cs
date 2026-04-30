using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces.Tools;
using VoiceAgent.Application.Tools;

namespace VoiceAgent.Infrastructure.Tools.Restaurant;

public sealed class MenuItemSearchTool(IAppDbContext db) : IAgentTool
{
    public string Name => "MenuItemSearchTool";
    public IReadOnlyCollection<string> RequiredSlots => ["query"];

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken ct = default)
    {
        var query = context.Slots.TryGetValue("query", out var value) ? value?.ToString() : context.UserMessage;
        if (string.IsNullOrWhiteSpace(query)) return new ToolExecutionResult { Success = false, ToolName = Name, ErrorCode = "MISSING_QUERY", ErrorMessage = "Missing item query." };

        var items = await db.MenuItems.Where(x => x.TenantId == context.TenantId && x.ClientId == context.ClientId && x.IsActive && x.IsAvailable && x.Name.Contains(query))
            .Select(x => new { x.Name, x.BasePrice, x.Currency })
            .ToListAsync(ct);

        return new ToolExecutionResult { Success = true, ToolName = Name, Data = new() { ["items"] = items } };
    }
}
