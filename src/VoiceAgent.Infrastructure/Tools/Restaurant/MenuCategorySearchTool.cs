using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces.Tools;
using VoiceAgent.Application.Tools;

namespace VoiceAgent.Infrastructure.Tools.Restaurant;

public sealed class MenuCategorySearchTool(IAppDbContext db) : IAgentTool
{
    public string Name => "MenuCategorySearchTool";
    public IReadOnlyCollection<string> RequiredSlots => Array.Empty<string>();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken ct = default)
    {
        var categories = await db.MenuCategories
            .Where(x => x.TenantId == context.TenantId && x.ClientId == context.ClientId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.Name)
            .ToListAsync(ct);

        return new ToolExecutionResult { Success = true, ToolName = Name, Data = new() { ["categories"] = categories } };
    }
}
