using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces.Tools;
using VoiceAgent.Application.Tools;

namespace VoiceAgent.Infrastructure.Tools.Restaurant;

public sealed class ListDealsTool(IAppDbContext db) : IAgentTool
{
    public string Name => "ListDealsTool";
    public IReadOnlyCollection<string> RequiredSlots => Array.Empty<string>();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var deals = await db.RestaurantDeals.Where(x => x.TenantId == context.TenantId && x.ClientId == context.ClientId && x.IsActive && x.IsAvailable && (x.ValidFrom == null || x.ValidFrom <= now) && (x.ValidTo == null || x.ValidTo >= now))
            .Select(x => new { x.Name, x.Description, x.DealPrice, x.Currency })
            .ToListAsync(ct);

        return new ToolExecutionResult { Success = true, ToolName = Name, Data = new() { ["deals"] = deals } };
    }
}
