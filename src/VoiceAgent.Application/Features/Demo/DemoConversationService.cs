using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Application.Features.Demo;

public class DemoConversationService(IDemoDataAccess dataAccess) : IDemoConversationService
{
    public async Task<DemoTurnResponse> StartAsync(StartConversationRequest request, CancellationToken ct = default)
    {
        var session = new CallSession
        {
            Id = Guid.NewGuid(), TenantId = request.TenantId, ClientId = request.ClientId, CampaignId = request.CampaignId,
            CustomerName = request.CustomerName, Channel = CallChannel.WebText, Direction = CampaignDirection.Inbound,
            Status = CallStatus.InProgress, StartedAt = DateTime.UtcNow, CurrentState = ConversationState.Started
        };
        await dataAccess.AddSessionAsync(session, ct);
        var agentMessage = "Welcome! Ask me about restaurant menu/deals or courier delivery quotes.";
        await AddTurnPair(session.Id, "user", "start demo", "agent", agentMessage, ConversationState.Started, ct);
        return new DemoTurnResponse(session.Id, 1, "start demo", agentMessage, ConversationState.Started.ToString());
    }

    public async Task<DemoTurnResponse> SendAsync(SendMessageRequest request, CancellationToken ct = default)
    {
        var session = await dataAccess.GetSessionAsync(request.CallSessionId, ct) ?? throw new InvalidOperationException("Session not found.");
        var text = request.Message.ToLowerInvariant();
        var agent = "I can help with menu, deals, or courier quotes.";
        var state = session.CurrentState;

        if (text.Contains("menu") || text.Contains("burger") || text.Contains("pizza"))
        {
            var items = await dataAccess.GetMenuItemsAsync(session.TenantId, session.ClientId, ct);
            agent = items.Count == 0 ? "Menu is currently unavailable." : $"Available menu: {string.Join(", ", items.Take(4).Select(x => $"{x.Name} ({x.Currency} {x.BasePrice:0.00})"))}.";
            state = ConversationState.IntentDetection;
        }
        else if (text.Contains("deal") || text.Contains("offer"))
        {
            var deals = await dataAccess.GetDealsAsync(session.TenantId, session.ClientId, ct);
            agent = deals.Count == 0 ? "No active deals right now." : $"Current deals: {string.Join("; ", deals.Take(3).Select(x => $"{x.Name} at {x.Currency} {x.DealPrice:0.00}"))}.";
            state = ConversationState.ExecutingTool;
        }
        else if (text.Contains("courier") || text.Contains("delivery") || text.Contains("quote"))
        {
            var profile = await dataAccess.GetCourierProfileAsync(session.TenantId, session.ClientId, ct);
            if (profile is null) agent = "Courier profile is not configured.";
            else
            {
                const decimal sampleDistance = 8; const decimal sampleWeight = 2;
                var estimate = Math.Max(profile.MinimumFee, profile.BaseFee + sampleDistance * profile.PricePerKm + sampleWeight * profile.PricePerKg);
                agent = $"Estimated courier fee for 8km/2kg is {profile.Currency} {estimate:0.00}.";
            }
            state = ConversationState.ExecutingTool;
        }

        session.CurrentState = state;
        var turnNumber = await AddTurnPair(session.Id, "user", request.Message, "agent", agent, state, ct);
        return new DemoTurnResponse(session.Id, turnNumber, request.Message, agent, state.ToString());
    }

    private async Task<int> AddTurnPair(Guid sessionId, string userSpeaker, string userText, string agentSpeaker, string agentText, ConversationState state, CancellationToken ct)
    {
        var turn = await dataAccess.GetNextTurnAsync(sessionId, ct);
        await dataAccess.AddTurnAsync(new CallTurn { Id = Guid.NewGuid(), CallSessionId = sessionId, TurnNumber = turn, Speaker = userSpeaker, Text = userText, StateAfter = state.ToString() }, ct);
        await dataAccess.AddTurnAsync(new CallTurn { Id = Guid.NewGuid(), CallSessionId = sessionId, TurnNumber = turn + 1, Speaker = agentSpeaker, Text = agentText, StateAfter = state.ToString() }, ct);
        await dataAccess.SaveChangesAsync(ct);
        return turn + 1;
    }
}
