using VoiceAgent.Application.Providers;

namespace VoiceAgent.Application.Services.Sales;

public sealed class OutboundDialerOrchestrator(
    IOutboundCampaignRuntimeReader campaignRuntimeReader,
    IOutboundAttemptWriter attemptWriter,
    IRetryEligibilityPolicy retryEligibilityPolicy,
    ITelephonyProvider telephonyProvider) : IOutboundDialerOrchestrator
{
    public async Task<OutboundDialCycleResult> RunCycleAsync(CancellationToken cancellationToken)
    {
        var campaign = await campaignRuntimeReader.GetNextActiveCampaignAsync(cancellationToken);
        if (campaign is null)
        {
            return new OutboundDialCycleResult(false, false, false, "NoActiveCampaign", null, null, null);
        }

        if (campaign.WarningBlocked)
        {
            return new OutboundDialCycleResult(true, false, false, "CampaignWarningBlocked", campaign.CampaignId, null, null);
        }

        var lead = await campaignRuntimeReader.GetNextEligibleLeadAsync(campaign, cancellationToken);
        if (lead is null)
        {
            return new OutboundDialCycleResult(true, false, false, "NoEligibleLead", campaign.CampaignId, null, null);
        }

        if (!retryEligibilityPolicy.CanRetry(lead, campaign, DateTime.UtcNow))
        {
            return new OutboundDialCycleResult(true, false, false, "RetryRuleBlocked", campaign.CampaignId, lead.LeadId, null);
        }

        var attempt = await attemptWriter.CreateAttemptAsync(
            new OutboundAttemptCreateRequest(
                campaign.TenantId,
                campaign.ClientId,
                campaign.CampaignId,
                lead.LeadId,
                lead.AttemptCount + 1),
            cancellationToken);

        await attemptWriter.RecordCallOutcomeAsync(attempt.AttemptId, "DialInitiated", null, cancellationToken);

        // Provider abstraction call placeholder. Infrastructure adapter will map this
        // request to a concrete telephony provider in the implementation phase.
        _ = telephonyProvider;

        return new OutboundDialCycleResult(true, true, true, "DialInitiated", campaign.CampaignId, lead.LeadId, attempt.AttemptId);
    }
}
