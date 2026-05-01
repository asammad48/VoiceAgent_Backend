using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Demo;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Application.Interfaces.Providers;
using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Application.Services;

public class ConversationOrchestratorService(IAppDbContext db, IGeocodingProvider geocodingProvider, IRoutingProvider routingProvider) : IConversationOrchestratorService
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

        if (lower.StartsWith("add ") || lower.Contains("addon") || lower.Contains("extra "))
        {
            if (cart.Items.Count == 0) return ("Please add an item first, then I can add toppings.", cart, null);
            var addonName = original.Replace("add", "", StringComparison.OrdinalIgnoreCase).Trim().TrimEnd('.');
            var lastItem = cart.Items.Last();
            var menuItem = items.FirstOrDefault(i => i.Name.Equals(lastItem.ItemName, StringComparison.OrdinalIgnoreCase));
            if (menuItem is null) return ("I couldn’t find that item in your cart.", cart, null);

            var addon = await db.MenuItemAddons.FirstOrDefaultAsync(x =>
                x.TenantId == session.TenantId &&
                x.ClientId == session.ClientId &&
                x.IsAvailable &&
                (x.MenuItemId == null || x.MenuItemId == menuItem.Id) &&
                addonName.Contains(x.Name, StringComparison.OrdinalIgnoreCase), ct);

            if (addon is null) return ($"I couldn’t find an addon called {addonName}.", cart, null);

            lastItem.UnitPrice += addon.Price;
            session.CollectedSlotsJson = JsonSerializer.Serialize(cart);
            return ($"Added {addon.Name} to {lastItem.ItemName}.", cart, null);
        }

        if (lower.Contains("total"))
        {
            var deliveryFee = cart.FulfillmentType == "delivery" ? 3.99m : 0m;
            var total = cart.Items.Sum(x => x.LineTotal) + deliveryFee;
            var currency = cart.Items.FirstOrDefault()?.Currency ?? "USD";
            return ($"Your total is {total:0.##} {currency}.", cart, null);
        }

        if (lower.Contains("delivery"))
        {
            cart.FulfillmentType = "delivery";
            session.CollectedSlotsJson = JsonSerializer.Serialize(cart);
            return ("Got it — delivery selected. I’ll apply a delivery fee.", cart, null);
        }

        if (lower.Contains("pickup"))
        {
            cart.FulfillmentType = "pickup";
            session.CollectedSlotsJson = JsonSerializer.Serialize(cart);
            return ("Pickup selected.", cart, null);
        }

        if (lower.Contains("pay cash") || lower.Contains("cash"))
        {
            cart.PaymentMethod = "cash";
            session.CollectedSlotsJson = JsonSerializer.Serialize(cart);
            return ("Cash payment noted.", cart, null);
        }

        if (lower.Contains("pay card") || lower.Contains("card"))
        {
            cart.PaymentMethod = "card";
            session.CollectedSlotsJson = JsonSerializer.Serialize(cart);
            return ("Card payment noted.", cart, null);
        }

        if (lower.Contains("confirm"))
        {
            if (cart.Items.Count == 0) return ("Your cart is empty right now.", cart, null);
            var subtotal = cart.Items.Sum(x => x.LineTotal);
            var deliveryFee = cart.FulfillmentType == "delivery" ? 3.99m : 0m;
            var total = subtotal + deliveryFee;
            var currency = cart.Items.FirstOrDefault()?.Currency ?? "USD";
            var order = new RestaurantOrder
            {
                Id = Guid.NewGuid(),
                TenantId = session.TenantId,
                ClientId = session.ClientId,
                CampaignId = session.CampaignId,
                CallSessionId = session.Id,
                FulfillmentType = cart.FulfillmentType ?? "pickup",
                ItemsJson = JsonSerializer.Serialize(cart.Items),
                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                Total = total,
                Currency = currency,
                Status = "Confirmed"
            };
            db.RestaurantOrders.Add(order);
            var final = new { type = "restaurant_order", orderId = order.Id, subtotal, deliveryFee, total, currency, payment = cart.PaymentMethod ?? "unknown" };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            return ($"Confirmed. Your order total is {total:0.##} {currency}.", cart, final);
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
        var lower = message.ToLowerInvariant();

        if (lower.Contains("what") && (lower.Contains("service") || lower.Contains("offer")))
        {
            return ("We offer same-day, standard, and fragile parcel courier service.", new List<string>(), null, null);
        }

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

        var distanceKm = details.DistanceKm ?? await ResolveDistanceKmAsync(details, ct) ?? 8m;
        details.DistanceKm = distanceKm;
        var profile = await db.CourierPricingProfiles.FirstOrDefaultAsync(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive, ct);
        if (profile is null)
        {
            return ("I couldn’t find pricing yet. Please try again shortly.", new List<string>(), null, null);
        }

        var urgencyFee = details.Urgency == "same_day" ? 5m : 0m;
        var fragileFee = details.IsFragile ? 2m : 0m;
        var total = Math.Max(profile.MinimumFee, profile.BaseFee + (profile.PricePerKm * distanceKm) + (profile.PricePerKg * details.WeightKg!.Value) + urgencyFee + fragileFee);
        var quote = new { details.Pickup, details.Dropoff, weightKg = details.WeightKg, distanceKm, urgency = details.Urgency, fragile = details.IsFragile, total, currency = profile.Currency };
        session.FinalResultJson = JsonSerializer.Serialize(quote);
        if (lower.Contains("confirm"))
        {
            var courierQuote = new CourierQuote
            {
                Id = Guid.NewGuid(),
                TenantId = session.TenantId,
                ClientId = session.ClientId,
                CampaignId = session.CampaignId,
                CallSessionId = session.Id,
                PickupAddressJson = JsonSerializer.Serialize(new { address = details.Pickup }),
                DropoffAddressJson = JsonSerializer.Serialize(new { address = details.Dropoff }),
                DistanceKm = distanceKm,
                WeightKg = details.WeightKg.Value,
                PackageType = details.IsFragile ? "fragile" : "standard",
                Urgency = details.Urgency ?? "standard",
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(details.Urgency == "same_day" ? 2 : 24),
                BaseFee = profile.BaseFee,
                DistanceFee = profile.PricePerKm * distanceKm,
                WeightFee = profile.PricePerKg * details.WeightKg.Value,
                UrgencyFee = urgencyFee + fragileFee,
                Total = total,
                Currency = profile.Currency,
                Status = "Quoted"
            };
            db.CourierQuotes.Add(courierQuote);
            var order = new CourierOrder
            {
                Id = Guid.NewGuid(),
                TenantId = session.TenantId,
                ClientId = session.ClientId,
                CampaignId = session.CampaignId,
                CallSessionId = session.Id,
                CourierQuoteId = courierQuote.Id,
                FinalResultJson = JsonSerializer.Serialize(quote),
                Status = "Confirmed"
            };
            db.CourierOrders.Add(order);
            var final = new { quoteId = courierQuote.Id, orderId = order.Id, total, currency = profile.Currency };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            return ($"Booking confirmed. Total is {total:0.##} {profile.Currency}.", new List<string>(), quote, final);
        }

        return ($"Your quote is {total:0.##} {profile.Currency}. Want me to confirm it?", new List<string>(), quote, quote);
    }

    private static bool TryExtractQuantityAndItem(string input, out int quantity, out string item)
    {
        var words = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
            ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10
        };
        var wordMatch = Regex.Match(input, @"\b(?<qty>one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<item>[a-zA-Z][a-zA-Z\s]+)", RegexOptions.IgnoreCase);
        if (wordMatch.Success && words.TryGetValue(wordMatch.Groups["qty"].Value, out quantity))
        {
            item = wordMatch.Groups["item"].Value.Trim();
            return true;
        }

        var match = Regex.Match(input, @"\b(?<qty>\d+)\s+(?<item>[a-zA-Z][a-zA-Z\s]+)", RegexOptions.IgnoreCase);
        quantity = 0; item = string.Empty;
        if (!match.Success) return false;
        quantity = int.Parse(match.Groups["qty"].Value);
        item = match.Groups["item"].Value.Trim();
        return true;
    }

    private static void ExtractCourier(string input, CourierSlots slots)
    {
        var fromTo = Regex.Match(input, @"from\s+(?<pickup>[\w\s]+?)\s+to\s+(?<dropoff>[\w\s]+?)(?:,|$)", RegexOptions.IgnoreCase);
        if (fromTo.Success)
        {
            slots.Pickup = fromTo.Groups["pickup"].Value.Trim();
            slots.Dropoff = fromTo.Groups["dropoff"].Value.Trim();
        }
        var pickup = Regex.Match(input, @"pickup\s+(?:from\s+)?(?<v>[\w\s]+?)(?:\s+to\s+|,|$)", RegexOptions.IgnoreCase);
        if (pickup.Success) slots.Pickup = pickup.Groups["v"].Value.Trim();

        var drop = Regex.Match(input, @"(?:dropoff|deliver|to)\s+(?<v>[\w\s]+?)(?:,|$)", RegexOptions.IgnoreCase);
        if (drop.Success) slots.Dropoff = drop.Groups["v"].Value.Trim();

        var weight = Regex.Match(input, @"(?<w>\d+(?:\.\d+)?)\s*(kg|kilogram)", RegexOptions.IgnoreCase);
        if (weight.Success) slots.WeightKg = decimal.Parse(weight.Groups["w"].Value);
        if (input.Contains("same day", StringComparison.OrdinalIgnoreCase)) slots.Urgency = "same_day";
        if (input.Contains("fragile", StringComparison.OrdinalIgnoreCase)) slots.IsFragile = true;
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

    private sealed class RestaurantCart { public List<CartItem> Items { get; set; } = new(); public string? FulfillmentType { get; set; } public string? PaymentMethod { get; set; } }
    private sealed class CartItem { public string ItemName { get; set; } = string.Empty; public int Quantity { get; set; } public decimal UnitPrice { get; set; } public string Currency { get; set; } = "USD"; public decimal LineTotal => Quantity * UnitPrice; }
    private sealed class CourierSlots { public string? Pickup { get; set; } public string? Dropoff { get; set; } public decimal? WeightKg { get; set; } public decimal? DistanceKm { get; set; } public string? Urgency { get; set; } public bool IsFragile { get; set; } }

    private async Task<decimal?> ResolveDistanceKmAsync(CourierSlots slots, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slots.Pickup) || string.IsNullOrWhiteSpace(slots.Dropoff)) return null;
        var from = await geocodingProvider.GeocodeAsync(slots.Pickup, ct);
        var to = await geocodingProvider.GeocodeAsync(slots.Dropoff, ct);
        if (from is null || to is null) return null;
        return await routingProvider.GetDistanceKmAsync(from.Value, to.Value, ct);
    }
}
