using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.DTOs.Demo;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Application.Services;

public class ConversationOrchestratorService(IAppDbContext db) : IConversationOrchestratorService
{
    public async Task<SendDemoMessageResponseDto> ProcessMessageAsync(Guid callSessionId, string message, CancellationToken ct = default)
    {
        var session = await db.CallSessions.FirstOrDefaultAsync(x => x.Id == callSessionId, ct)
            ?? throw new InvalidOperationException("Call session not found.");

        var client = await db.Clients.FirstOrDefaultAsync(x => x.Id == session.ClientId && x.TenantId == session.TenantId, ct)
            ?? throw new InvalidOperationException("Client not found.");

        var campaign = await db.Campaigns.FirstOrDefaultAsync(x => x.Id == session.CampaignId && x.ClientId == session.ClientId && x.TenantId == session.TenantId, ct)
            ?? throw new InvalidOperationException("Campaign not found.");

        _ = await db.CampaignConfigurations.FirstOrDefaultAsync(x => x.CampaignId == campaign.Id && x.ClientId == campaign.ClientId && x.TenantId == campaign.TenantId && x.IsActive, ct);

        var turnNumber = await db.CallTurns.CountAsync(x => x.CallSessionId == session.Id, ct) + 1;
        db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber, Speaker = "user", Text = message, StateBefore = session.CurrentState.ToString() });

        var lower = message.ToLowerInvariant();
        string reply;
        var missingSlots = new List<string>();
        object? cart = null;
        object? quote = null;
        object? finalResult = null;

        switch (campaign.CampaignType)
        {
            case CampaignType.RestaurantOrder:
                (reply, cart, finalResult) = await HandleRestaurantAsync(session, campaign, lower, message, ct);
                break;
            case CampaignType.CourierService:
                (reply, missingSlots, quote, finalResult) = await HandleCourierAsync(session, client, message, ct);
                break;
            case CampaignType.CabBooking:
            case CampaignType.DoctorAppointment:
            case CampaignType.MedicareSales:
            case CampaignType.AcaSales:
            case CampaignType.FeSales:
                reply = $"Got it. {client.AgentName} can help with {campaign.CampaignType}. What would you like to do first?";
                break;
            default:
                reply = "Thanks. Could you share a bit more detail?";
                break;
        }

        db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = reply, StateAfter = session.CurrentState.ToString() });
        session.CurrentState = finalResult is null ? ConversationState.CollectingSlots : ConversationState.SavingResult;

        await db.SaveChangesAsync(ct);

        return new SendDemoMessageResponseDto
        {
            Reply = reply,
            CurrentState = session.CurrentState.ToString(),
            MissingSlots = missingSlots,
            CurrentCart = cart,
            CurrentQuote = quote,
            FinalResult = finalResult
        };
    }

    public Task<string> OrchestrateAsync(Guid callSessionId, string message, CancellationToken ct = default)
        => ProcessMessageAsync(callSessionId, message, ct).ContinueWith(x => x.Result.Reply, ct);

    private async Task<(string Reply, object? Cart, object? FinalResult)> HandleRestaurantAsync(CallSession session, Campaign campaign, string lower, string original, CancellationToken ct)
    {
        var mentionAgent = await db.Clients.Where(c => c.Id == session.ClientId).Select(c => c.AgentName).FirstOrDefaultAsync(ct) ?? "I";
        var cart = ParseCart(session.CollectedSlotsJson);

        if (lower.Contains("menu") || lower.Contains("what do you have") || lower.Contains("categories"))
        {
            var categories = await db.MenuCategories.Where(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive)
                .OrderBy(x => x.SortOrder).Select(x => x.Name).ToListAsync(ct);
            var text = categories.Count == 0 ? "I don’t have menu categories yet." : $"We have: {string.Join(", ", categories.Take(6))}.";
            return (text, cart, null);
        }

        if (lower.Contains("deals") || lower.Contains("offers") || lower.Contains("combo"))
        {
            var now = DateTime.UtcNow;
            var deals = await db.RestaurantDeals.Where(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive && x.IsAvailable && (x.ValidFrom == null || x.ValidFrom <= now) && (x.ValidTo == null || x.ValidTo >= now))
                .Select(x => new { x.Name, x.DealPrice, x.Currency }).ToListAsync(ct);
            var text = deals.Count == 0 ? "No active deals right now." : string.Join("; ", deals.Take(3).Select(d => $"{d.Name} {d.DealPrice:0.##} {d.Currency}"));
            return (text, cart, null);
        }

        var items = await db.MenuItems.Where(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive && x.IsAvailable).ToListAsync(ct);
        var matched = items.FirstOrDefault(i => lower.Contains(i.Name.ToLowerInvariant()));

        if (TryExtractQuantityAndItem(original, out var qty, out var extractedItem))
        {
            var menuMatch = items.FirstOrDefault(i => i.Name.Contains(extractedItem, StringComparison.OrdinalIgnoreCase));
            if (menuMatch is not null)
            {
                AddToCart(cart, menuMatch.Name, qty, menuMatch.BasePrice, menuMatch.Currency);
                session.CollectedSlotsJson = JsonSerializer.Serialize(cart);
                var ready = cart.Items.Count > 0;
                var final = ready ? new { type = "restaurant_order", items = cart.Items, total = cart.Items.Sum(x => x.LineTotal), currency = cart.Items.First().Currency } : null;
                if (final is not null) session.FinalResultJson = JsonSerializer.Serialize(final);
                return ($"Done. Added {qty} {menuMatch.Name}. Anything else?", cart, final);
            }
        }

        if (lower.Contains("total"))
        {
            var total = cart.Items.Sum(x => x.LineTotal);
            var currency = cart.Items.FirstOrDefault()?.Currency ?? "USD";
            return ($"Your total is {total:0.##} {currency}.", cart, null);
        }

        if (matched is not null)
        {
            return ($"{matched.Name} is available at {matched.BasePrice:0.##} {matched.Currency}. How many would you like?", cart, null);
        }

        return ($"Hey, this is {mentionAgent}. Want menu categories, deals, or a specific item?", cart, null);
    }

    private async Task<(string Reply, List<string> MissingSlots, object? Quote, object? FinalResult)> HandleCourierAsync(CallSession session, Client client, string message, CancellationToken ct)
    {
        var details = ParseCourierSlots(session.CollectedSlotsJson);
        ExtractCourier(message, details);

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(details.Pickup)) missing.Add("pickup");
        if (string.IsNullOrWhiteSpace(details.Dropoff)) missing.Add("dropoff");
        if (!details.WeightKg.HasValue) missing.Add("weight");

        session.CollectedSlotsJson = JsonSerializer.Serialize(details);

        if (missing.Count > 0)
        {
            var ask = missing[0] switch
            {
                "pickup" => "Where should we pick it up?",
                "dropoff" => "Where should we deliver it?",
                _ => "What’s the package weight in kg?"
            };
            return ($"{client.AgentName} here. {ask}", missing, null, null);
        }

        var distanceKm = details.DistanceKm ?? 8m;
        var profile = await db.CourierPricingProfiles.FirstOrDefaultAsync(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive, ct);
        if (profile is null)
        {
            return ("I couldn’t find pricing yet. Please try again shortly.", new List<string>(), null, null);
        }

        var total = Math.Max(profile.MinimumFee, profile.BaseFee + (profile.PricePerKm * distanceKm) + (profile.PricePerKg * details.WeightKg!.Value));
        var quote = new { details.Pickup, details.Dropoff, weightKg = details.WeightKg, distanceKm, total, currency = profile.Currency };
        session.FinalResultJson = JsonSerializer.Serialize(quote);
        return ($"Your quote is {total:0.##} {profile.Currency}. Want me to confirm it?", new List<string>(), quote, quote);
    }

    private static bool TryExtractQuantityAndItem(string input, out int quantity, out string item)
    {
        var match = Regex.Match(input, @"\b(?<qty>\d+)\s+(?<item>[a-zA-Z][a-zA-Z\s]+)", RegexOptions.IgnoreCase);
        quantity = 0;
        item = string.Empty;
        if (!match.Success) return false;
        quantity = int.Parse(match.Groups["qty"].Value);
        item = match.Groups["item"].Value.Trim();
        return true;
    }

    private static void ExtractCourier(string input, CourierSlots slots)
    {
        var pickup = Regex.Match(input, @"pickup\s+(?:from\s+)?(?<v>[\w\s]+?)(?:\s+to\s+|,|$)", RegexOptions.IgnoreCase);
        if (pickup.Success) slots.Pickup = pickup.Groups["v"].Value.Trim();

        var drop = Regex.Match(input, @"(?:dropoff|deliver|to)\s+(?<v>[\w\s]+?)(?:,|$)", RegexOptions.IgnoreCase);
        if (drop.Success) slots.Dropoff = drop.Groups["v"].Value.Trim();

        var weight = Regex.Match(input, @"(?<w>\d+(?:\.\d+)?)\s*(kg|kilogram)", RegexOptions.IgnoreCase);
        if (weight.Success) slots.WeightKg = decimal.Parse(weight.Groups["w"].Value);
    }

    private static RestaurantCart ParseCart(string? json)
        => string.IsNullOrWhiteSpace(json) ? new RestaurantCart() : JsonSerializer.Deserialize<RestaurantCart>(json) ?? new RestaurantCart();

    private static CourierSlots ParseCourierSlots(string? json)
        => string.IsNullOrWhiteSpace(json) ? new CourierSlots() : JsonSerializer.Deserialize<CourierSlots>(json) ?? new CourierSlots();

    private static void AddToCart(RestaurantCart cart, string itemName, int qty, decimal unitPrice, string currency)
    {
        var existing = cart.Items.FirstOrDefault(x => x.ItemName.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (existing is null) cart.Items.Add(new CartItem { ItemName = itemName, Quantity = qty, UnitPrice = unitPrice, Currency = currency });
        else existing.Quantity += qty;
    }

    private sealed class RestaurantCart { public List<CartItem> Items { get; set; } = new(); }
    private sealed class CartItem { public string ItemName { get; set; } = string.Empty; public int Quantity { get; set; } public decimal UnitPrice { get; set; } public string Currency { get; set; } = "USD"; public decimal LineTotal => Quantity * UnitPrice; }
    private sealed class CourierSlots { public string? Pickup { get; set; } public string? Dropoff { get; set; } public decimal? WeightKg { get; set; } public decimal? DistanceKm { get; set; } }
}
