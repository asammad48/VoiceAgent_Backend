using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Campaigns;
using VoiceAgent.Application.Dtos.Demo;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Application.Services;

public class DemoConversationService(IAppDbContext db, IConversationOrchestratorService orchestrator) : IDemoConversationService
{
    public async Task<IReadOnlyList<CampaignResponseDto>> GetDemoCampaignsAsync(CancellationToken ct = default)
    {
        return await db.Campaigns
            .Where(x => x.IsActive && x.IsDemoEnabled)
            .OrderBy(x => x.Name)
            .Select(x => new CampaignResponseDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                ClientId = x.ClientId,
                BranchId = x.BranchId,
                Name = x.Name,
                CampaignType = x.CampaignType.ToString(),
                Direction = x.Direction.ToString(),
                IsActive = x.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<StartDemoConversationResponseDto> StartAsync(StartDemoConversationRequestDto request, CancellationToken ct = default)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(x =>
            x.Id == request.CampaignId &&
            x.TenantId == request.TenantId &&
            x.ClientId == request.ClientId &&
            x.IsActive &&
            x.IsDemoEnabled, ct) ?? throw new InvalidOperationException("Demo campaign not found or disabled.");

        var channel = Enum.TryParse<CallChannel>(request.Channel, true, out var parsed) ? parsed : CallChannel.WebText;
        var now = DateTime.UtcNow;
        var session = new CallSession
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ClientId = request.ClientId,
            CampaignId = request.CampaignId,
            Channel = channel,
            Direction = CampaignDirection.WebDemo,
            Status = CallStatus.InProgress,
            CurrentState = ConversationState.Greeting,
            CorrelationId = Guid.NewGuid().ToString("N"),
            CreatedOn = now,
            StartedAt = now,
            HandoffAllowed = true
        };

        var greeting = campaign.CampaignType switch
        {
            CampaignType.RestaurantOrder => "Hi! I can help with menu items, deals, or your order.",
            CampaignType.CourierService => "Hi! Share pickup, dropoff, and package weight to get a quote.",
            _ => "Hi! How can I help you today?"
        };

        db.CallSessions.Add(session);
        db.CallTurns.Add(new CallTurn
        {
            Id = Guid.NewGuid(),
            CallSessionId = session.Id,
            TurnNumber = 1,
            Speaker = "bot",
            Text = greeting,
            StateAfter = session.CurrentState.ToString(),
            CreatedOn = now
        });

        await db.SaveChangesAsync(ct);

        return new StartDemoConversationResponseDto
        {
            CallSessionId = session.Id,
            Message = greeting,
            CurrentState = session.CurrentState.ToString()
        };
    }

    public Task<SendDemoMessageResponseDto> SendAsync(SendDemoMessageRequestDto request, CancellationToken ct = default)
        => orchestrator.ProcessMessageAsync(request.CallSessionId, request.Message, ct);

    public async Task<bool> EndAsync(Guid callSessionId, CancellationToken ct = default)
    {
        var session = await db.CallSessions.FirstOrDefaultAsync(x => x.Id == callSessionId, ct);
        if (session is null) return false;

        session.Status = CallStatus.Completed;
        session.EndedAt = DateTime.UtcNow;
        session.DurationSeconds = (int)Math.Max(0, (session.EndedAt.Value - session.StartedAt).TotalSeconds);
        session.CurrentState = ConversationState.Completed;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
