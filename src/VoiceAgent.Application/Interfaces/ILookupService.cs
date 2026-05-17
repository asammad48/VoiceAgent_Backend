namespace VoiceAgent.Application.Interfaces;

public sealed record LookupContext(Guid TenantId, Guid ClientId, Guid CampaignId, Guid CallSessionId);

public interface ILookupService
{
    Task<LookupResult> ExecuteAsync(string intentId, Dictionary<string, string> slots, LookupContext context, CancellationToken ct = default);
}

public sealed record LookupResult(string Message, bool OffersContinue, string? ContinueToIntentId);
