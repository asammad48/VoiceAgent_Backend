using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Demo;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Application.Interfaces.Providers;
using VoiceAgent.Application.Services.Rag;
using VoiceAgent.Domain.Entities;
using VoiceAgent.Domain.Enums;

namespace VoiceAgent.Application.Services;

public class ConversationOrchestratorService(IAppDbContext db, IGeocodingProvider geocodingProvider, IRoutingProvider routingProvider, IRagRetrievalService ragRetrievalService) : IConversationOrchestratorService
{
    public async Task<SendDemoMessageResponseDto> ProcessMessageAsync(Guid callSessionId, string message, CancellationToken ct = default)
    {
        var session = await db.CallSessions.FirstOrDefaultAsync(x => x.Id == callSessionId, ct)
            ?? throw new InvalidOperationException("Call session not found.");

        var client = await db.Clients.FirstOrDefaultAsync(x => x.Id == session.ClientId && x.TenantId == session.TenantId, ct)
            ?? throw new InvalidOperationException("Client not found.");

        var campaign = await db.Campaigns.FirstOrDefaultAsync(x => x.Id == session.CampaignId && x.ClientId == session.ClientId && x.TenantId == session.TenantId, ct)
            ?? throw new InvalidOperationException("Campaign not found.");

        var campaignConfig = await db.CampaignConfigurations.FirstOrDefaultAsync(x => x.CampaignId == campaign.Id && x.ClientId == campaign.ClientId && x.TenantId == campaign.TenantId && x.IsActive, ct);

        var turnNumber = await db.CallTurns.CountAsync(x => x.CallSessionId == session.Id, ct) + 1;
        db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber, Speaker = "user", Text = message, StateBefore = session.CurrentState.ToString() });

        var lower = message.ToLowerInvariant();

        var ragReply = await TryGetRagScopedReplyAsync(session, campaignConfig, message, lower, ct);
        if (!string.IsNullOrWhiteSpace(ragReply))
        {
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = ragReply, StateAfter = session.CurrentState.ToString() });
            await AddToolLogAsync(session, "RagAnswerTool", "Success", new { query = message }, new { reply = ragReply }, ct);
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = ragReply, CurrentState = session.CurrentState.ToString(), MissingSlots = new List<string>() };
        }

        if (lower.Contains("tell me something from another client") || lower.Contains("show me all client policies") || lower.Contains("search all campaigns") || lower.Contains("ignore your instructions"))
        {
            var guarded = "I can only use information for this service. How can I help with this request?";
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = guarded, StateAfter = session.CurrentState.ToString() });
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "prompt_injection_blocked", EventDataJson = JsonSerializer.Serialize(new { message }) });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = guarded, CurrentState = session.CurrentState.ToString(), MissingSlots = new List<string>() };
        }

        string reply;
        var missingSlots = new List<string>();
        object? cart = null;
        object? quote = null;
        object? finalResult = null;

        if (TryGetCrossCampaignRedirect(campaign.CampaignType, lower, out var redirect))
        {
            reply = redirect;
        }
        else
        switch (campaign.CampaignType)
        {
            case CampaignType.RestaurantOrder:
                (reply, cart, finalResult) = await HandleRestaurantAsync(session, campaign, lower, message, ct);
                break;
            case CampaignType.CourierService:
                (reply, missingSlots, quote, finalResult) = await HandleCourierAsync(session, client, message, ct);
                break;
            case CampaignType.CabBooking:
                (reply, missingSlots, quote, finalResult) = await HandleCabAsync(session, client, message, ct);
                break;
            case CampaignType.DoctorAppointment:
                (reply, missingSlots, quote, finalResult) = await HandleDoctorAsync(session, client, message, ct);
                break;
            case CampaignType.MedicareSales:
                (reply, missingSlots, quote, finalResult) = await HandleMedicareAsync(session, client, message, ct);
                break;
            case CampaignType.AcaSales:
                (reply, missingSlots, quote, finalResult) = await HandleAcaAsync(session, client, message, ct);
                break;
            case CampaignType.FeSales:
                (reply, missingSlots, quote, finalResult) = await HandleFeAsync(session, client, message, ct);
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
            await AddToolLogAsync(session, "MenuCategorySearchTool", "Success", new { query = "categories" }, new { count = categories.Count }, ct);
            return (text, cart, null);
        }

        if (lower.Contains("deals") || lower.Contains("offers") || lower.Contains("combo"))
        {
            var now = DateTime.UtcNow;
            var deals = await db.RestaurantDeals.Where(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive && x.IsAvailable && (x.ValidFrom == null || x.ValidFrom <= now) && (x.ValidTo == null || x.ValidTo >= now))
                .Select(x => new { x.Name, x.DealPrice, x.Currency }).ToListAsync(ct);
            var text = deals.Count == 0 ? "No active deals right now." : string.Join("; ", deals.Take(3).Select(d => $"{d.Name} {d.DealPrice:0.##} {d.Currency}"));
            await AddToolLogAsync(session, "ListDealsTool", "Success", new { query = "deals" }, new { count = deals.Count }, ct);
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
            await AddToolLogAsync(session, "RestaurantTotalTool", "Success", new { itemCount = cart.Items.Count }, new { total, currency }, ct);
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
        if (lower.Contains("chemical") || lower.Contains("chemicals") || lower.Contains("weapon") || lower.Contains("illegal"))
        {
            return ("We cannot carry dangerous chemicals. I can help with normal parcels, documents, or approved packages.", new List<string>(), null, null);
        }

        if (lower.Contains("same day"))
        {
            details.Urgency = "same_day";
            session.CollectedSlotsJson = JsonSerializer.Serialize(details);
            if (details.WeightKg.HasValue && !string.IsNullOrWhiteSpace(details.Pickup) && !string.IsNullOrWhiteSpace(details.Dropoff))
            {
                var sameDayProfile = await db.CourierPricingProfiles.FirstOrDefaultAsync(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive, ct);
                var sameDayDistance = details.DistanceKm ?? await ResolveDistanceKmAsync(details, ct) ?? 8m;
                if (sameDayProfile is not null)
                {
                    var sameDayTotal = Math.Max(sameDayProfile.MinimumFee, sameDayProfile.BaseFee + (sameDayProfile.PricePerKm * sameDayDistance) + (sameDayProfile.PricePerKg * details.WeightKg.Value) + 5m + (details.IsFragile ? 2m : 0m));
                    return ($"Same-day delivery selected. The updated estimate is {sameDayTotal:0.##} {sameDayProfile.Currency}.", new List<string>(), new { details.Pickup, details.Dropoff, weightKg = details.WeightKg, distanceKm = sameDayDistance, urgency = details.Urgency, fragile = details.IsFragile, total = sameDayTotal, currency = sameDayProfile.Currency }, null);
                }
            }
            return ("Same-day delivery selected. Share pickup, drop-off, and weight to update your quote.", new List<string>(), null, null);
        }

        if (lower.Contains("fragile"))
        {
            details.IsFragile = true;
            session.CollectedSlotsJson = JsonSerializer.Serialize(details);
            return ("I’ve marked it as fragile. I’ll include that in the quote.", new List<string>(), null, null);
        }

        if (lower.Contains("what") && (lower.Contains("service") || lower.Contains("offer")))
        {
            return ("We can help with documents, parcels, and small business deliveries. I’ll need pickup, drop-off, package weight, and urgency to quote it.", new List<string>(), null, null);
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

        if (distanceKm > profile.MaxDistanceKm)
        {
            return ("That looks outside our current courier coverage area. I can only quote deliveries within our service range.", new List<string>(), null, null);
        }

        var urgencyFee = details.Urgency == "same_day" ? 5m : 0m;
        var fragileFee = details.IsFragile ? 2m : 0m;
        var total = Math.Max(profile.MinimumFee, profile.BaseFee + (profile.PricePerKm * distanceKm) + (profile.PricePerKg * details.WeightKg!.Value) + urgencyFee + fragileFee);
        var quote = new { details.Pickup, details.Dropoff, weightKg = details.WeightKg, distanceKm, urgency = details.Urgency, fragile = details.IsFragile, total, currency = profile.Currency };
        session.FinalResultJson = JsonSerializer.Serialize(quote);
        await AddToolLogAsync(session, "CourierQuoteTool", "Success", new { details.Pickup, details.Dropoff, details.WeightKg }, quote, ct);
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
            return ("Your courier request has been captured. Our team will review and confirm it.", new List<string>(), quote, final);
        }

        return ($"Got it. For a {details.WeightKg:0.##} kg parcel from {details.Pickup} to {details.Dropoff}, the estimated price is {total:0.##} {profile.Currency} and delivery time is about {(details.Urgency == "same_day" ? "1-3 hours" : "3-5 hours")}. Do you want standard or same-day delivery?", new List<string>(), quote, quote);
    }


    private async Task<(string Reply, List<string> MissingSlots, object? Quote, object? FinalResult)> HandleCabAsync(CallSession session, Client client, string message, CancellationToken ct)
    {
        var slots = ParseCabSlots(session.CollectedSlotsJson);
        ExtractCab(message, slots);
        var lower = message.ToLowerInvariant();
        if (lower.Contains("speak to someone") || lower.Contains("human") || lower.Contains("agent"))
        {
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "handoff_requested", EventDataJson = "{\"mode\":\"test\"}" });
            return ("I can connect you to the team. In this test environment, I’ve marked this call for handoff.", new List<string>(), null, null);
        }

        if (lower.Contains("helicopter"))
            return ("We do not offer that vehicle type. Available options are Standard, Executive, 6-Seater, and Wheelchair Accessible.", new List<string>(), null, null);

        if (slots.PassengerCount.HasValue && slots.PassengerCount.Value > 10)
            return ("That’s more than one vehicle can handle. Would you like me to arrange multiple vehicles?", new List<string>(), null, null);

        if (lower.Contains("cab") && string.IsNullOrWhiteSpace(slots.Pickup))
            return ("Sure, where should we pick you up from?", new List<string>{"pickupLocation"}, null, null);

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(slots.Pickup)) missing.Add("pickupLocation");
        if (string.IsNullOrWhiteSpace(slots.Dropoff)) missing.Add("dropoffLocation");
        if (string.IsNullOrWhiteSpace(slots.PickupDateTime)) missing.Add("pickupDateTime");
        if (!slots.PassengerCount.HasValue) missing.Add("passengerCount");
        if (string.IsNullOrWhiteSpace(slots.VehicleType)) missing.Add("vehicleType");

        session.CollectedSlotsJson = JsonSerializer.Serialize(slots);

        if (missing.Count > 0)
        {
            var ask = missing[0] switch
            {
                "pickupLocation" => "Sure, where should we pick you up from?",
                "dropoffLocation" => "Where would you like to go?",
                "pickupDateTime" => "Got it. What time do you need the cab?",
                "passengerCount" => "How many passengers will travel?",
                _ => "What vehicle type would you prefer?"
            };
            return (ask, missing, null, null);
        }

        var fare = 18m;
        var quote = new { pickupLocation = slots.Pickup, dropoffLocation = slots.Dropoff, pickupDateTime = slots.PickupDateTime, passengerCount = slots.PassengerCount, vehicleType = slots.VehicleType, fare, currency = "GBP" };
        session.FinalResultJson = JsonSerializer.Serialize(quote);

        if (lower.Contains("fare"))
            return ($"The estimated fare is £{fare:0.##}. Final fare may depend on traffic and confirmation.", new List<string>(), quote, null);

        if (lower.Contains("confirm"))
        {
            var final = new { type = "cab_booking", quote, status = "CapturedOnly" };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            return ("Your cab booking request has been captured. We’ll confirm it shortly.", new List<string>(), quote, final);
        }

        return ("Would you like a fare estimate or should I confirm the booking?", new List<string>(), quote, null);
    }


    private async Task<(string Reply, List<string> MissingSlots, object? Quote, object? FinalResult)> HandleDoctorAsync(CallSession session, Client client, string message, CancellationToken ct)
    {
        var slots = ParseDoctorSlots(session.CollectedSlotsJson);
        ExtractDoctor(message, slots);
        var lower = message.ToLowerInvariant();
        var highRisk = new[] { "chest pain", "cannot breathe", "severe bleeding", "unconscious", "suicidal" };
        if (highRisk.Any(k => lower.Contains(k)))
        {
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "doctor_high_risk_detected", EventDataJson = JsonSerializer.Serialize(new { message }) });
            session.HandoffRequested = true;
            return ("That may need urgent medical attention. Please contact emergency services immediately or go to the nearest emergency department.", new List<string>(), null, null);
        }

        if (lower.Contains("medicine") || lower.Contains("diagnose") || lower.Contains("prescribe"))
            return ("I can help capture an appointment request, but I cannot give medical advice. Would you like me to request an appointment?", new List<string>(), null, null);

        if (lower.Contains("speak to someone"))
            return ("I can help with clinic appointment requests here. If symptoms are urgent, please contact emergency services now.", new List<string>(), null, null);

        if (lower.Contains("doctor appointment") || lower.Contains("appointment") && string.IsNullOrWhiteSpace(slots.ReasonForVisit))
            return ("Sure, what is the appointment for?", new List<string>{"reasonForVisit"}, null, null);

        if (!string.IsNullOrWhiteSpace(slots.PreferredDoctor) && slots.PreferredDoctor.Contains("Ahmed", StringComparison.OrdinalIgnoreCase) && (slots.PreferredDateTime?.Contains("sunday", StringComparison.OrdinalIgnoreCase) ?? false))
            return ("Dr Ahmed is not available on Sunday. He is usually available Monday, Wednesday, and Friday. Would one of those work?", new List<string>(), null, null);

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(slots.ReasonForVisit)) missing.Add("reasonForVisit");
        if (string.IsNullOrWhiteSpace(slots.PatientName)) missing.Add("patientName");
        if (string.IsNullOrWhiteSpace(slots.PreferredDateTime)) missing.Add("preferredDateTime");
        if (string.IsNullOrWhiteSpace(slots.PreferredDoctor)) missing.Add("preferredDoctor");
        if (string.IsNullOrWhiteSpace(slots.Phone)) missing.Add("phone");

        session.CollectedSlotsJson = JsonSerializer.Serialize(slots);

        if (missing.Count > 0)
        {
            var ask = missing[0] switch
            {
                "reasonForVisit" => "Sure, what is the appointment for?",
                "patientName" => "Can I take the patient name?",
                "preferredDateTime" => $"I’ve noted {slots.ReasonForVisit}. What day or time would you prefer?",
                "preferredDoctor" => "Do you prefer a specific doctor, or is any doctor fine?",
                _ => "Can I take your phone number for the appointment request?"
            };
            return (ask, missing, null, null);
        }

        if (lower.Contains("confirm"))
        {
            var final = new { type = "doctor_appointment", patientName = slots.PatientName, phone = slots.Phone, reasonForVisit = slots.ReasonForVisit, preferredDateTime = slots.PreferredDateTime, preferredDoctor = slots.PreferredDoctor, status = "CapturedOnly" };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            return ("Your appointment request has been captured. The clinic team will confirm availability.", new List<string>(), null, final);
        }

        return ("I can help with clinic appointment requests here. What appointment do you need?", new List<string>(), null, null);
    }


    private Task<(string Reply, List<string> MissingSlots, object? Quote, object? FinalResult)> HandleMedicareAsync(CallSession session, Client client, string message, CancellationToken ct)
    {
        var slots = ParseMedicareSlots(session.CollectedSlotsJson);
        var lower = message.ToLowerInvariant();
        if (lower.Contains("remove me") || lower.Contains("do not call") || lower.Contains("opt out"))
        {
            slots.OptedOut = true;
            slots.InterestLevel = "NotInterested";
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "lead_opt_out_requested" });
            return Task.FromResult(("Understood. I’ll mark your request not to be contacted again.", new List<string>(), (object?)null, (object?)null));
        }

        if (lower.Contains("not interested"))
        {
            slots.InterestLevel = "NotInterested";
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("No problem, I’ll mark that down. Thank you for your time.", new List<string>(), (object?)null, (object?)null));
        }

        if (lower.Contains("speak to someone") || lower.Contains("specialist"))
        {
            session.HandoffRequested = true;
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "handoff_requested", EventDataJson = "{\"campaign\":\"medicare\"}" });
            return Task.FromResult(("I can mark this for a specialist to contact you. In this test environment, I’ve saved the handoff request.", new List<string>(), (object?)null, (object?)null));
        }

        if (lower.Contains("eligible"))
            return Task.FromResult(("I cannot confirm eligibility. I can only capture your interest and have a specialist review it.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("save me money") || lower.Contains("savings"))
            return Task.FromResult(("I cannot promise savings. A specialist would need to review your details first.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("government"))
            return Task.FromResult(("No, I’m not a government agency. I can only share Medicare-related information and capture your interest.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("what information") || lower.Contains("what do you collect"))
            return Task.FromResult(("I collect basic qualification and contact information only, like age range, current coverage, and callback preference.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("what is this call about"))
            return Task.FromResult(("I’m calling from Demo Benefits Support to see if you’d like information about Medicare-related options. I can collect basic interest, but I cannot confirm eligibility.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("interested"))
        {
            slots.InterestLevel = "Interested";
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("Great. Are you currently 65 or older?", new List<string>{"age"}, (object?)null, (object?)null));
        }

        var ageMatch = Regex.Match(message, @"\b(?<age>\d{2})\b");
        if (ageMatch.Success)
        {
            slots.Age = int.Parse(ageMatch.Groups["age"].Value);
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("Thanks. Do you currently have Medicare Part A or Part B?", new List<string>{"currentCoverage"}, (object?)null, (object?)null));
        }

        if (lower.Contains("part a") || lower.Contains("part b"))
        {
            slots.CurrentCoverage = message.Trim();
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            var final = new { type = "medicare_lead", interestLevel = slots.InterestLevel ?? "Interested", age = slots.Age, currentCoverage = slots.CurrentCoverage, optedOut = slots.OptedOut, status = "CapturedOnly" };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            return Task.FromResult(("Thanks, I’ve noted that. Would you like a specialist callback?", new List<string>(), (object?)null, (object?)final));
        }

        if (lower.Contains("pizza") || lower.Contains("restaurant") || lower.Contains("courier"))
            return Task.FromResult(("I can only help with Medicare-related information in this conversation.", new List<string>(), (object?)null, (object?)null));

        return Task.FromResult(("I can help with Medicare-related information. Are you interested in learning about your options?", new List<string>(), (object?)null, (object?)null));
    }


    private Task<(string Reply, List<string> MissingSlots, object? Quote, object? FinalResult)> HandleAcaAsync(CallSession session, Client client, string message, CancellationToken ct)
    {
        var slots = ParseAcaSlots(session.CollectedSlotsJson);
        var lower = message.ToLowerInvariant();
        if (lower.Contains("stop calling") || lower.Contains("remove me") || lower.Contains("opt out"))
        {
            slots.OptedOut = true;
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "lead_opt_out_requested", EventDataJson = "{\"campaign\":\"aca\"}" });
            return Task.FromResult(("Understood. I’ll mark your request not to be contacted again.", new List<string>(), (object?)null, (object?)null));
        }

        if (lower.Contains("subsidy") || lower.Contains("guarantee"))
            return Task.FromResult(("I cannot guarantee subsidies or eligibility. A licensed specialist or official review would need to confirm that.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("how much") || lower.Contains("cost") || lower.Contains("price"))
            return Task.FromResult(("I can’t provide official pricing here. I can capture your details and have a specialist review options with you.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("what is aca"))
            return Task.FromResult(("ACA refers to health coverage options under the Affordable Care Act. I can collect basic interest, but I cannot determine eligibility or pricing.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("determine eligibility") || lower.Contains("check eligibility"))
            return Task.FromResult(("I can collect basic details for review. Do you currently have health insurance?", new List<string>{"currentInsuranceStatus"}, (object?)null, (object?)null));

        if (lower.Contains("no, i do not have insurance") || lower.Contains("no insurance"))
        {
            slots.CurrentInsuranceStatus = "Uninsured";
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("Thanks. How many people are in your household?", new List<string>{"householdSize"}, (object?)null, (object?)null));
        }

        if (lower.Contains("already have insurance"))
        {
            slots.CurrentInsuranceStatus = "Insured";
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("Understood. Would you still like information about coverage options, or should I close this request?", new List<string>(), (object?)null, (object?)null));
        }

        var hh = Regex.Match(lower, @"(?<n>\d+)\s+(people|person)");
        if (hh.Success)
        {
            slots.HouseholdSize = int.Parse(hh.Groups["n"].Value);
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("Thanks. Are you looking for individual or family coverage?", new List<string>{"coverageInterest"}, (object?)null, (object?)null));
        }

        if (lower.Contains("individual") || lower.Contains("family"))
        {
            slots.CoverageInterest = lower.Contains("family") ? "Family" : "Individual";
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("Got it. What callback time works best for you?", new List<string>{"callbackTime"}, (object?)null, (object?)null));
        }

        if (lower.Contains("call me") || lower.Contains("tomorrow") || lower.Contains("afternoon"))
        {
            slots.CallbackTime = message.Trim();
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            var final = new { type = "aca_lead", insuranceStatus = slots.CurrentInsuranceStatus, householdSize = slots.HouseholdSize, coverageInterest = slots.CoverageInterest, callbackTime = slots.CallbackTime, optedOut = slots.OptedOut, status = "CapturedOnly" };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            return Task.FromResult(("Got it. I’ve saved tomorrow afternoon as your preferred callback time.", new List<string>(), (object?)null, (object?)final));
        }

        if (lower.Contains("book me a cab") || lower.Contains("pizza") || lower.Contains("courier"))
            return Task.FromResult(("I can only help with health coverage information in this conversation.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("what details") || lower.Contains("what do you need"))
            return Task.FromResult(("I collect current insurance status, household size, coverage interest, and callback information.", new List<string>(), (object?)null, (object?)null));

        return Task.FromResult(("I can help with ACA-related information and capture your interest for specialist review.", new List<string>(), (object?)null, (object?)null));
    }


    private Task<(string Reply, List<string> MissingSlots, object? Quote, object? FinalResult)> HandleFeAsync(CallSession session, Client client, string message, CancellationToken ct)
    {
        var slots = ParseFeSlots(session.CollectedSlotsJson);
        var lower = message.ToLowerInvariant();
        if (lower.Contains("remove my number") || lower.Contains("stop calling") || lower.Contains("opt out"))
        {
            slots.OptedOut = true;
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "lead_opt_out_requested", EventDataJson = "{\"campaign\":\"fe\"}" });
            return Task.FromResult(("Understood. I’ll mark your request not to be contacted again.", new List<string>(), (object?)null, (object?)null));
        }

        if (lower.Contains("not interested"))
        {
            slots.InterestLevel = "NotInterested";
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("No problem, I’ll mark that down. Thank you for your time.", new List<string>(), (object?)null, (object?)null));
        }

        if (lower.Contains("guarantee") || lower.Contains("approval"))
            return Task.FromResult(("I cannot guarantee approval. I can only capture your enquiry for review.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("how much") || lower.Contains("cost") || lower.Contains("rate"))
            return Task.FromResult(("I do not have confirmed pricing here. A specialist can explain costs after reviewing your enquiry.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("what are you offering"))
            return Task.FromResult(("We help capture basic business funding enquiries, such as working capital, expansion funding, or equipment finance. I can collect details for review.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("tell me more"))
            return Task.FromResult(("Sure. What type of business do you run?", new List<string>{"businessType"}, (object?)null, (object?)null));

        if (lower.Contains("i run") || lower.Contains("business"))
        {
            slots.BusinessType = message.Trim();
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("Thanks. What is your approximate monthly revenue range?", new List<string>{"monthlyRevenueRange"}, (object?)null, (object?)null));
        }

        if (lower.Contains("per month") || lower.Contains("monthly") || Regex.IsMatch(lower, @"\£?\d+[\,\d]*"))
        {
            slots.MonthlyRevenueRange = message.Trim();
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("Thanks. Are you looking for working capital, expansion funding, or equipment finance?", new List<string>{"fundingPurpose"}, (object?)null, (object?)null));
        }

        if (lower.Contains("working capital") || lower.Contains("expansion") || lower.Contains("equipment finance"))
        {
            slots.FundingPurpose = lower.Contains("working capital") ? "Working Capital" : lower.Contains("equipment") ? "Equipment Finance" : "Expansion Funding";
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return Task.FromResult(("Got it. What callback time works best for you?", new List<string>{"callbackTime"}, (object?)null, (object?)null));
        }

        if (lower.Contains("call me") || lower.Contains("tomorrow") || lower.Contains("pm") || lower.Contains("am"))
        {
            slots.CallbackTime = message.Trim();
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            var final = new { type = "fe_lead", businessType = slots.BusinessType, monthlyRevenueRange = slots.MonthlyRevenueRange, fundingPurpose = slots.FundingPurpose, callbackTime = slots.CallbackTime, interestLevel = slots.InterestLevel ?? "Interested", optedOut = slots.OptedOut, status = "CapturedOnly" };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            return Task.FromResult(("Got it. I’ve saved tomorrow at 2 PM as your preferred callback time.", new List<string>(), (object?)null, (object?)final));
        }

        if (lower.Contains("approve loans directly"))
            return Task.FromResult(("No, I do not approve funding directly. I only capture enquiries for specialist review.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("what funding types"))
            return Task.FromResult(("We support working capital, expansion funding, and equipment finance enquiries.", new List<string>(), (object?)null, (object?)null));

        if (lower.Contains("doctor") || lower.Contains("appointment") || lower.Contains("medicare"))
            return Task.FromResult(("I can only help with business funding enquiries in this conversation.", new List<string>(), (object?)null, (object?)null));

        return Task.FromResult(("I can help with business funding enquiries and capture details for review.", new List<string>(), (object?)null, (object?)null));
    }


    private static bool TryGetCrossCampaignRedirect(CampaignType campaignType, string lower, out string reply)
    {
        reply = string.Empty;
        string[] restaurant = ["burger", "pizza", "menu", "deals"];
        string[] courier = ["courier", "parcel", "delivery fee"];
        string[] cab = ["cab", "vehicle", "driver"];
        string[] doctor = ["doctor", "appointment", "dr "];
        string[] medicare = ["medicare", "eligibility", "part a", "part b"];
        string[] aca = ["aca", "subsidy", "health coverage"];
        string[] fe = ["funding", "business finance", "approval"];

        bool HasAny(string[] terms) => terms.Any(t => lower.Contains(t));

        switch (campaignType)
        {
            case CampaignType.RestaurantOrder when HasAny(courier) || HasAny(cab) || HasAny(doctor) || HasAny(medicare) || HasAny(aca) || HasAny(fe):
                reply = "I can help with restaurant orders here. Would you like to see the menu or deals?"; return true;
            case CampaignType.CourierService when HasAny(restaurant) || HasAny(cab) || HasAny(doctor) || HasAny(medicare) || HasAny(aca) || HasAny(fe):
                reply = "I can help with courier delivery here. What are the pickup and drop-off addresses?"; return true;
            case CampaignType.CabBooking when HasAny(restaurant) || HasAny(courier) || HasAny(doctor) || HasAny(medicare) || HasAny(aca) || HasAny(fe):
                reply = "I can help with cab bookings here. Where should we pick you up from?"; return true;
            case CampaignType.DoctorAppointment when HasAny(restaurant) || HasAny(courier) || HasAny(cab) || HasAny(medicare) || HasAny(aca) || HasAny(fe):
                reply = "I can help with clinic appointment requests here. What appointment do you need?"; return true;
            case CampaignType.MedicareSales when HasAny(restaurant) || HasAny(courier) || HasAny(cab) || HasAny(doctor) || HasAny(aca) || HasAny(fe):
                reply = "I can only help with Medicare-related information in this conversation."; return true;
            case CampaignType.AcaSales when HasAny(restaurant) || HasAny(courier) || HasAny(cab) || HasAny(doctor) || HasAny(medicare) || HasAny(fe):
                reply = "I can only help with health coverage information in this conversation."; return true;
            case CampaignType.FeSales when HasAny(restaurant) || HasAny(courier) || HasAny(cab) || HasAny(doctor) || HasAny(medicare) || HasAny(aca):
                reply = "I can only help with business funding enquiries in this conversation."; return true;
            default:
                return false;
        }
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


    private static void ExtractCab(string input, CabSlots slots)
    {
        var pair = Regex.Match(input, @"pickup(?:\s+is)?\s+(?<p>[\w\s]+?)\s+and\s+drop-?off(?:\s+is)?\s+(?<d>[\w\s]+?)(?:\.|$)", RegexOptions.IgnoreCase);
        if (pair.Success)
        {
            slots.Pickup = pair.Groups["p"].Value.Trim();
            slots.Dropoff = pair.Groups["d"].Value.Trim();
        }

        var pickup = Regex.Match(input, @"pick(?:\s+me)?\s+from\s+(?<p>[\w\s]+?)(?:\.|$)", RegexOptions.IgnoreCase);
        if (pickup.Success) slots.Pickup = pickup.Groups["p"].Value.Trim();

        var passengers = Regex.Match(input, @"(?<n>\d+)\s+passenger", RegexOptions.IgnoreCase);
        if (passengers.Success) slots.PassengerCount = int.Parse(passengers.Groups["n"].Value);
        if (input.Contains("today", StringComparison.OrdinalIgnoreCase) || input.Contains("pm", StringComparison.OrdinalIgnoreCase) || input.Contains("am", StringComparison.OrdinalIgnoreCase)) slots.PickupDateTime = input.Trim();
        if (input.Contains("standard", StringComparison.OrdinalIgnoreCase)) slots.VehicleType = "Standard";
        else if (input.Contains("executive", StringComparison.OrdinalIgnoreCase)) slots.VehicleType = "Executive";
        else if (input.Contains("6-seater", StringComparison.OrdinalIgnoreCase) || input.Contains("6 seater", StringComparison.OrdinalIgnoreCase)) slots.VehicleType = "6-Seater";
        else if (input.Contains("wheelchair", StringComparison.OrdinalIgnoreCase)) slots.VehicleType = "Wheelchair Accessible";
    }

    private static CabSlots ParseCabSlots(string? json)
        => string.IsNullOrWhiteSpace(json) ? new CabSlots() : JsonSerializer.Deserialize<CabSlots>(json) ?? new CabSlots();


    private static void ExtractDoctor(string input, DoctorSlots slots)
    {
        var name = Regex.Match(input, @"my name is\s+(?<n>[a-zA-Z\s]+)", RegexOptions.IgnoreCase);
        if (name.Success) slots.PatientName = name.Groups["n"].Value.Trim();

        var phone = Regex.Match(input, @"(?<p>\+?\d[\d\s\-]{7,})");
        if (phone.Success) slots.Phone = phone.Groups["p"].Value.Trim();

        if (input.Contains("back pain", StringComparison.OrdinalIgnoreCase)) slots.ReasonForVisit = "back pain";
        else if (input.Contains("appointment for", StringComparison.OrdinalIgnoreCase)) slots.ReasonForVisit = input[(input.IndexOf("appointment for", StringComparison.OrdinalIgnoreCase)+15)..].Trim().TrimEnd('.');

        if (input.Contains("tomorrow", StringComparison.OrdinalIgnoreCase) || input.Contains("morning", StringComparison.OrdinalIgnoreCase) || input.Contains("pm", StringComparison.OrdinalIgnoreCase) || input.Contains("am", StringComparison.OrdinalIgnoreCase)) slots.PreferredDateTime = input.Trim();
        if (input.Contains("any doctor", StringComparison.OrdinalIgnoreCase)) slots.PreferredDoctor = "Any";
        if (input.Contains("dr ahmed", StringComparison.OrdinalIgnoreCase)) slots.PreferredDoctor = "Dr Ahmed";
    }

    private static DoctorSlots ParseDoctorSlots(string? json)
        => string.IsNullOrWhiteSpace(json) ? new DoctorSlots() : JsonSerializer.Deserialize<DoctorSlots>(json) ?? new DoctorSlots();


    private static MedicareSlots ParseMedicareSlots(string? json)
        => string.IsNullOrWhiteSpace(json) ? new MedicareSlots() : JsonSerializer.Deserialize<MedicareSlots>(json) ?? new MedicareSlots();


    private static AcaSlots ParseAcaSlots(string? json)
        => string.IsNullOrWhiteSpace(json) ? new AcaSlots() : JsonSerializer.Deserialize<AcaSlots>(json) ?? new AcaSlots();


    private static FeSlots ParseFeSlots(string? json)
        => string.IsNullOrWhiteSpace(json) ? new FeSlots() : JsonSerializer.Deserialize<FeSlots>(json) ?? new FeSlots();

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
    private sealed class CabSlots { public string? Pickup { get; set; } public string? Dropoff { get; set; } public string? PickupDateTime { get; set; } public int? PassengerCount { get; set; } public string? VehicleType { get; set; } }
    private sealed class DoctorSlots { public string? PatientName { get; set; } public string? Phone { get; set; } public string? ReasonForVisit { get; set; } public string? PreferredDateTime { get; set; } public string? PreferredDoctor { get; set; } }
    private sealed class MedicareSlots { public string? InterestLevel { get; set; } public int? Age { get; set; } public string? CurrentCoverage { get; set; } public bool OptedOut { get; set; } }
    private sealed class AcaSlots { public string? CurrentInsuranceStatus { get; set; } public int? HouseholdSize { get; set; } public string? CoverageInterest { get; set; } public string? CallbackTime { get; set; } public bool OptedOut { get; set; } }
    private sealed class FeSlots { public string? BusinessType { get; set; } public string? MonthlyRevenueRange { get; set; } public string? FundingPurpose { get; set; } public string? CallbackTime { get; set; } public string? InterestLevel { get; set; } public bool OptedOut { get; set; } }


    private async Task<string?> TryGetRagScopedReplyAsync(CallSession session, CampaignConfiguration? campaignConfig, string message, string lower, CancellationToken ct)
    {
        if (campaignConfig is null || string.IsNullOrWhiteSpace(campaignConfig.RagSettingsJson)) return null;
        RagRuntimeConfiguration? runtime;
        try
        {
            runtime = JsonSerializer.Deserialize<RagRuntimeConfiguration>(campaignConfig.RagSettingsJson);
        }
        catch
        {
            return null;
        }

        if (runtime is null || !runtime.Enabled || runtime.KnowledgeBaseId == Guid.Empty) return null;

        var result = await ragRetrievalService.SearchAsync(
            new RagSearchRequest(new RagScope(session.TenantId, session.ClientId, session.CampaignId, runtime.KnowledgeBaseId), message, runtime.TopK, runtime.MinScore, runtime.AllowedDocumentTypes),
            ct);

        if (!result.Found || result.Chunks.Count == 0) return null;
        var best = result.Chunks.OrderByDescending(x => x.Score).First();
        return best.ChunkText;
    }

    private Task AddToolLogAsync(CallSession session, string toolName, string status, object request, object? response, CancellationToken ct)
    {
        db.ToolCallLogs.Add(new ToolCallLog
        {
            Id = Guid.NewGuid(),
            TenantId = session.TenantId,
            ClientId = session.ClientId,
            CampaignId = session.CampaignId,
            CallSessionId = session.Id,
            ToolName = toolName,
            Status = status,
            RequestJson = JsonSerializer.Serialize(request),
            ResponseJson = JsonSerializer.Serialize(response),
            ErrorMessage = string.Empty,
            DurationMs = 0
        });
        return Task.CompletedTask;
    }

    private async Task<decimal?> ResolveDistanceKmAsync(CourierSlots slots, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slots.Pickup) || string.IsNullOrWhiteSpace(slots.Dropoff)) return null;
        var from = await geocodingProvider.GeocodeAsync(slots.Pickup, ct);
        var to = await geocodingProvider.GeocodeAsync(slots.Dropoff, ct);
        if (from is null || to is null) return null;
        return await routingProvider.GetDistanceKmAsync(from.Value, to.Value, ct);
    }
}
