using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces.Tools;
using VoiceAgent.Application.Tools;

namespace VoiceAgent.Infrastructure.Tools.Courier;

public sealed class CourierQuoteTool(IAppDbContext db) : IAgentTool
{
    public string Name => "CourierQuoteTool";
    public IReadOnlyCollection<string> RequiredSlots => ["weightKg"];

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken ct = default)
    {
        var profile = await db.CourierPricingProfiles.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.ClientId == context.ClientId && x.IsActive, ct);
        if (profile is null)
        {
            return new ToolExecutionResult { Success = false, ToolName = Name, ErrorCode = "PROFILE_NOT_FOUND", ErrorMessage = "No active courier pricing profile found." };
        }

        var weight = context.Slots.TryGetValue("weightKg", out var w) && decimal.TryParse(w?.ToString(), out var wt) ? wt : 0m;
        var distanceKm = context.Slots.TryGetValue("distanceKm", out var d) && decimal.TryParse(d?.ToString(), out var dist) ? dist : 8m;
        var total = Math.Max(profile.MinimumFee, profile.BaseFee + (profile.PricePerKm * distanceKm) + (profile.PricePerKg * weight));

        return new ToolExecutionResult { Success = true, ToolName = Name, Data = new() { ["quote"] = new { distanceKm, weightKg = weight, total, currency = profile.Currency } } };
    }
}
