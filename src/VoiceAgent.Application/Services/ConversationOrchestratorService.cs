using System.Text.Json;
using System.Text.Json.Serialization;
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

public class ConversationOrchestratorService(
    IAppDbContext db,
    IGeocodingProvider geocodingProvider,
    IRoutingProvider routingProvider,
    IRagRetrievalService ragRetrievalService) : IConversationOrchestratorService
{
    // ── Entry points ──────────────────────────────────────────────────────────

    public async Task<SendDemoMessageResponseDto> ProcessMessageAsync(Guid callSessionId, string message, CancellationToken ct = default)
    {
        var session = await db.CallSessions.FirstOrDefaultAsync(x => x.Id == callSessionId, ct)
            ?? throw new InvalidOperationException("Call session not found.");

        var client = await db.Clients.FirstOrDefaultAsync(x => x.Id == session.ClientId && x.TenantId == session.TenantId, ct)
            ?? throw new InvalidOperationException("Client not found.");

        var campaign = await db.Campaigns.FirstOrDefaultAsync(x => x.Id == session.CampaignId && x.ClientId == session.ClientId && x.TenantId == session.TenantId, ct)
            ?? throw new InvalidOperationException("Campaign not found.");

        var config = await db.CampaignConfigurations.FirstOrDefaultAsync(x => x.CampaignId == campaign.Id && x.TenantId == campaign.TenantId && x.IsActive, ct);

        var turnNumber = await db.CallTurns.CountAsync(x => x.CallSessionId == session.Id, ct) + 1;
        db.CallTurns.Add(new CallTurn
        {
            Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber,
            Speaker = "user", Text = message, StateBefore = session.CurrentState.ToString()
        });

        // ── RAG scoped reply (knowledge-base override) ─────────────────────
        var ragReply = await TryGetRagScopedReplyAsync(session, config, message, ct);
        if (!string.IsNullOrWhiteSpace(ragReply))
        {
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = ragReply, StateAfter = session.CurrentState.ToString() });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = ragReply, CurrentState = session.CurrentState.ToString(), MissingSlots = [] };
        }

        // ── Prompt injection guard ─────────────────────────────────────────
        var lower = message.ToLowerInvariant();
        if (lower.Contains("tell me something from another client") || lower.Contains("show me all client policies") || lower.Contains("ignore your instructions"))
        {
            var guarded = "I can only use information for this service. How can I help you?";
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = guarded, StateAfter = session.CurrentState.ToString() });
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "prompt_injection_blocked", EventDataJson = JsonSerializer.Serialize(new { message }) });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = guarded, CurrentState = session.CurrentState.ToString(), MissingSlots = [] };
        }

        // ── Universal opt-out / objection intercepts ───────────────────────
        var optOutReply = TryHandleOptOut(session, lower, db);
        if (optOutReply is not null)
        {
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = optOutReply, StateAfter = session.CurrentState.ToString() });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = optOutReply, CurrentState = session.CurrentState.ToString(), MissingSlots = [] };
        }

        // ── Cross-campaign guard ───────────────────────────────────────────
        if (TryGetCrossCampaignRedirect(campaign.CampaignType, lower, out var redirect))
        {
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = redirect, StateAfter = session.CurrentState.ToString() });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = redirect, CurrentState = session.CurrentState.ToString(), MissingSlots = [] };
        }

        // ── Main questionnaire engine ──────────────────────────────────────
        var (reply, missingSlots, finalResult) = await HandleQuestionnaireAsync(session, campaign, config, message, lower, ct);

        db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = reply, StateAfter = session.CurrentState.ToString() });
        session.CurrentState = finalResult is null ? ConversationState.CollectingSlots : ConversationState.SavingResult;

        await db.SaveChangesAsync(ct);

        return new SendDemoMessageResponseDto
        {
            Reply = reply,
            CurrentState = session.CurrentState.ToString(),
            MissingSlots = missingSlots,
            FinalResult = finalResult
        };
    }

    public Task<string> OrchestrateAsync(Guid callSessionId, string message, CancellationToken ct = default)
        => ProcessMessageAsync(callSessionId, message, ct).ContinueWith(t => t.Result.Reply, ct);

    // ── Questionnaire engine ──────────────────────────────────────────────────

    private async Task<(string Reply, List<string> MissingSlots, object? FinalResult)> HandleQuestionnaireAsync(
        CallSession session, Campaign campaign, CampaignConfiguration? config,
        string message, string lower, CancellationToken ct)
    {
        var questionnaire = TryParseQuestionnaire(config?.QuestionnaireJson);
        var slots = ParseSlots(session.CollectedSlotsJson);

        // Find the question the bot last asked (first unanswered required question)
        // and try to extract an answer ONLY for that question from the user's message.
        // This prevents free-text answers bleeding into the wrong slot.
        var currentQuestion = questionnaire.Questions
            .Where(q => q.Required && !slots.ContainsKey(q.Id))
            .OrderBy(q => q.Order)
            .FirstOrDefault();

        if (currentQuestion is not null)
        {
            var extracted = TryExtractValue(currentQuestion.Id, message, lower, currentQuestion.ValidValues);
            if (extracted is not null)
                slots[currentQuestion.Id] = extracted;
        }

        // Also extract typed slot types that are unambiguous regardless of which question is active
        // (phone numbers, yes/no flags). These can never bleed incorrectly.
        foreach (var q in questionnaire.Questions.OrderBy(q => q.Order))
        {
            if (slots.ContainsKey(q.Id)) continue;
            if (q.Id == currentQuestion?.Id) continue; // already handled above

            if (IsUniquelyTypedSlot(q.Id))
            {
                var v = TryExtractValue(q.Id, message, lower, q.ValidValues);
                if (v is not null) slots[q.Id] = v;
            }
        }

        session.CollectedSlotsJson = JsonSerializer.Serialize(slots);

        // Campaign-specific extras (cart building, pricing, availability)
        var extraResult = await HandleCampaignSpecificAsync(session, campaign, config, message, lower, slots, ct);
        if (extraResult is not null)
        {
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            return extraResult.Value;
        }

        // Recompute missing AFTER all extraction/cart-building
        var missing = questionnaire.Questions
            .Where(q => q.Required && !slots.ContainsKey(q.Id))
            .OrderBy(q => q.Order)
            .ToList();

        if (missing.Count > 0)
        {
            var next = missing.First();
            // If the user said something meaningful but we still couldn't extract this slot,
            // use a softer re-prompt so it sounds natural rather than repeating the same line.
            var userTriedToAnswer = currentQuestion?.Id == next.Id && IsMeaningfulResponse(lower);
            var questionText = userTriedToAnswer
                ? $"I'm sorry, I didn't quite catch that. {next.Question}"
                : next.Question;
            return (questionText, missing.Select(q => q.Id).ToList(), null);
        }

        // All required questions answered — build final result
        var finalResult = BuildFinalResult(campaign.CampaignType, slots, session);
        session.FinalResultJson = JsonSerializer.Serialize(finalResult);
        return (BuildConfirmation(campaign.CampaignType, slots), [], finalResult);
    }

    // Slots whose content is structurally unambiguous — safe to scan across all turns
    private static bool IsUniquelyTypedSlot(string id) => id is
        "phone" or "callbackPhone" or "age" or "state" or
        "tobaccoUse" or "healthConditions" or "interestConfirmed" or
        "fulfillmentType" or "paymentMethod" or "urgency" or "packageType" or "vehicleType";

    private static string? TryExtractValue(string slotId, string message, string lower, List<string>? validValues)
    {
        return slotId switch
        {
            // items is a JSON cart — never extracted generically; only set by HandleRestaurantExtrasAsync
            "items" => null,

            "firstName" or "customerName" or "leadName" or "patientName" or "beneficiaryName"
                => ExtractName(message, lower),

            "phone" or "callbackPhone"
                => ExtractPhone(message),

            "age"
                => ExtractAge(lower),

            "state"
                => ExtractState(lower),

            "householdSize" or "passengerCount"
                => ExtractNumber(lower),

            "currentInsuranceStatus" or "tobaccoUse" or "healthConditions" or "interestConfirmed"
                => ExtractYesNo(lower),

            "coverageInterest"
                => lower.Contains("family") ? "Family" : lower.Contains("individual") ? "Individual" : null,

            "fulfillmentType"
                => lower.Contains("delivery") ? "delivery" : lower.Contains("pickup") ? "pickup" : null,

            "paymentMethod"
                => lower.Contains("card") ? "card" : lower.Contains("cash") ? "cash" : null,

            "urgency"
                => lower.Contains("same day") || lower.Contains("same-day") ? "same_day" : lower.Contains("standard") ? "standard" : null,

            "packageType"
                => lower.Contains("fragile") ? "fragile" : lower.Contains("document") ? "document" : lower.Contains("standard") ? "standard" : null,

            "vehicleType"
                => lower.Contains("executive") ? "Executive"
                 : lower.Contains("6-seater") || lower.Contains("6 seater") ? "6-Seater"
                 : lower.Contains("wheelchair") ? "Wheelchair Accessible"
                 : lower.Contains("standard") ? "Standard"
                 : null,

            "callbackTime"
                => ExtractCallbackTime(lower),

            "ageRange"
                => lower.Contains("65") || lower.Contains("over 65") || lower.Contains("older") ? "65 or older"
                 : lower.Contains("approaching") || lower.Contains("turning 65") || lower.Contains("64") ? "approaching 65"
                 : lower.Contains("under") ? "under 65"
                 : null,

            "coverageAmount"
                => ExtractCoverageAmount(lower),

            "incomeRange" or "monthlyRevenueRange"
                => ExtractIncomeRange(message, lower),

            // For free-text slots (addresses, reasons, etc.) — use the full message if it's a genuine answer
            _ => IsMeaningfulResponse(lower) ? message.Trim() : null
        };
    }

    // ── Extraction helpers ────────────────────────────────────────────────────

    private static string? ExtractName(string message, string lower)
    {
        var patterns = new[]
        {
            @"my name is\s+(?<n>[A-Za-z][A-Za-z\s]+)",
            @"this is\s+(?<n>[A-Za-z][A-Za-z\s]+)",
            @"i'?m\s+(?<n>[A-Za-z][A-Za-z\s]{1,30})",
            @"it'?s\s+(?<n>[A-Za-z][A-Za-z\s]{1,30})",
            @"name(?:'?s)? is\s+(?<n>[A-Za-z][A-Za-z\s]+)",
            @"call me\s+(?<n>[A-Za-z][A-Za-z\s]{1,20})",
            @"^(?<n>[A-Z][a-z]{1,15}(?:\s+[A-Z][a-z]{1,20}){0,2})[.,!?]?$",  // bare "John" or "John Smith"
        };
        foreach (var pat in patterns)
        {
            var m = Regex.Match(message.Trim(), pat, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var candidate = m.Groups["n"].Value.Trim().TrimEnd('.');
                // Reject if the candidate is a common non-name word
                if (!NonNameWords.Contains(candidate.Split(' ')[0]))
                    return candidate;
            }
        }
        return null;
    }

    private static readonly HashSet<string> NonNameWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Yes","Yep","Yeah","Yup","Sure","Ok","Okay","No","Nope","Nah","Not",
        "You","Your","My","Me","I","It","Is","Are","Was","Be","Name","First",
        "The","A","An","This","That","These","Those","Please","Sorry","Hello",
        "Hi","Hey","Um","Uh","Hmm","Well","So","Just","Like","Good","Fine",
        "Right","True","Great","Call","Can","Thank","Thanks","And","Or","But",
        "What","Who","Where","When","How","Do","Did","Does","Have","Has","Had",
    };

    private static string? ExtractPhone(string message)
    {
        var m = Regex.Match(message, @"(?<p>\+?[\d][\d\s\-\(\)]{7,15})");
        return m.Success ? Regex.Replace(m.Groups["p"].Value, @"\s+", "") : null;
    }

    private static string? ExtractAge(string lower)
    {
        var m = Regex.Match(lower, @"\b(?<a>\d{2})\b");
        if (!m.Success) return null;
        var age = int.Parse(m.Groups["a"].Value);
        return age is >= 18 and <= 110 ? age.ToString() : null;
    }

    private static string? ExtractNumber(string lower)
    {
        var m = Regex.Match(lower, @"\b(?<n>\d{1,2})\b");
        return m.Success ? m.Groups["n"].Value : null;
    }

    private static string? ExtractYesNo(string lower)
    {
        if (Regex.IsMatch(lower, @"\b(yes|yeah|yep|yup|sure|correct|i do|i have|i am|absolutely|definitely)\b")) return "Yes";
        if (Regex.IsMatch(lower, @"\b(no|nope|nah|not|don'?t|haven'?t|i don'?t|i haven'?t|never)\b")) return "No";
        return null;
    }

    private static string? ExtractCallbackTime(string lower)
    {
        if (lower.Contains("morning") || Regex.IsMatch(lower, @"\b(8|9|10|11)\s*a")) return "Morning";
        if (lower.Contains("afternoon") || Regex.IsMatch(lower, @"\b(12|1|2|3|4|5)\s*p")) return "Afternoon";
        if (lower.Contains("evening") || lower.Contains("night") || Regex.IsMatch(lower, @"\b(6|7|8|9)\s*p")) return "Evening";
        // Specific time mention
        var time = Regex.Match(lower, @"\b\d{1,2}(?::\d{2})?\s*(?:am|pm)\b");
        return time.Success ? time.Value.ToUpperInvariant() : null;
    }

    private static string? ExtractCoverageAmount(string lower)
    {
        var m = Regex.Match(lower, @"\$?(?<n>\d[\d,]*)\s*(?:k|thousand)?");
        if (!m.Success) return null;
        var raw = m.Groups["n"].Value.Replace(",", "");
        if (!int.TryParse(raw, out var num)) return null;
        if (lower.Contains('k') || lower.Contains("thousand")) num *= 1000;
        return $"${num:N0}";
    }

    private static string? ExtractIncomeRange(string message, string lower)
    {
        if (lower.Contains("under") || lower.Contains("less than") || lower.Contains("below"))
        {
            var m = Regex.Match(lower, @"\$?(?<n>\d[\d,]*)\s*(?:k|thousand)?");
            if (m.Success) return $"Under {m.Value.Trim()}";
        }
        if (lower.Contains("over") || lower.Contains("more than") || lower.Contains("above"))
        {
            var m = Regex.Match(lower, @"\$?(?<n>\d[\d,]*)\s*(?:k|thousand)?");
            if (m.Success) return $"Over {m.Value.Trim()}";
        }
        var range = Regex.Match(lower, @"\$?(?<a>\d[\d,]*)\s*(?:k|thousand)?\s*(?:to|-)\s*\$?(?<b>\d[\d,]*)\s*(?:k|thousand)?");
        if (range.Success) return $"{range.Groups["a"].Value} to {range.Groups["b"].Value}";
        var single = Regex.Match(lower, @"\$?(?<n>\d[\d,]*)\s*(?:k|thousand)?");
        return single.Success ? $"~{single.Value.Trim()}" : null;
    }

    private static string? ExtractState(string lower)
    {
        foreach (var (name, abbrev) in States)
        {
            if (lower.Contains(name) || Regex.IsMatch(lower, $@"\b{abbrev}\b", RegexOptions.IgnoreCase))
                return name;
        }
        return null;
    }

    private static bool IsMeaningfulResponse(string lower)
    {
        var stripped = lower.Trim('.', '!', '?', ',');
        return stripped.Length >= 2
            && !Regex.IsMatch(stripped, @"^\s*(um+|uh+|hmm+|ok+|okay+|sure|yeah|yep|yup|nope|nah|no|hi|hello|hey)\s*$");
    }

    // ── Campaign-specific extras (cart building, pricing) ─────────────────────

    private async Task<(string Reply, List<string> MissingSlots, object? FinalResult)?> HandleCampaignSpecificAsync(
        CallSession session, Campaign campaign, CampaignConfiguration? config,
        string message, string lower, Dictionary<string, string> slots, CancellationToken ct)
    {
        return campaign.CampaignType switch
        {
            CampaignType.RestaurantOrder => await HandleRestaurantExtrasAsync(session, lower, message, slots, ct),
            CampaignType.CourierService  => await HandleCourierExtrasAsync(session, config, lower, slots, ct),
            CampaignType.CabBooking      => HandleCabExtras(lower, slots),
            CampaignType.DoctorAppointment => HandleDoctorExtras(lower, message, slots, session, config),
            _ => null
        };
    }

    private async Task<(string Reply, List<string> MissingSlots, object? FinalResult)?> HandleRestaurantExtrasAsync(
        CallSession session, string lower, string original, Dictionary<string, string> slots, CancellationToken ct)
    {
        var menuItems = await db.MenuItems
            .Where(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive && x.IsAvailable)
            .ToListAsync(ct);

        // ── Menu / categories ─────────────────────────────────────────────────
        if (lower.Contains("menu") || lower.Contains("categories") || lower.Contains("what do you have") || lower.Contains("what's available"))
        {
            var cats = await db.MenuCategories
                .Where(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive)
                .OrderBy(x => x.SortOrder).Select(x => x.Name).ToListAsync(ct);
            return cats.Count == 0
                ? ("We don't have menu categories set up yet.", [], null)
                : ($"We have: {string.Join(", ", cats.Take(6))}. What would you like?", [], null);
        }

        // ── Deals ─────────────────────────────────────────────────────────────
        if (lower.Contains("deals") || lower.Contains("offers") || lower.Contains("combo") || lower.Contains("special"))
        {
            var now = DateTime.UtcNow;
            var deals = await db.RestaurantDeals
                .Where(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive && x.IsAvailable
                         && (x.ValidFrom == null || x.ValidFrom <= now) && (x.ValidTo == null || x.ValidTo >= now))
                .Select(x => new { x.Name, x.DealPrice, x.Currency }).ToListAsync(ct);
            return deals.Count == 0
                ? ("No active deals right now.", [], null)
                : (string.Join("; ", deals.Take(3).Select(d => $"{d.Name} — {d.DealPrice:0.##} {d.Currency}")), [], null);
        }

        // ── Addon to last cart item ───────────────────────────────────────────
        if ((lower.StartsWith("add ") || lower.Contains("extra ") || lower.Contains("addon")) && !TryExtractQuantityAndItem(original, out _, out _))
        {
            var cart = ParseCart(slots.GetValueOrDefault("items"));
            if (cart.Count == 0)
                return ("Please add a menu item first, then I can add extras to it.", [], null);

            var addonName = Regex.Replace(original, @"^(add|extra|addon)\s+", "", RegexOptions.IgnoreCase).Trim().TrimEnd('.');
            var lastItem  = cart.Last();
            var lastMenuItem = menuItems.FirstOrDefault(i => i.Name.Equals(lastItem.Name, StringComparison.OrdinalIgnoreCase));

            var addon = await db.MenuItemAddons.FirstOrDefaultAsync(x =>
                x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsAvailable
                && (x.MenuItemId == null || x.MenuItemId == lastMenuItem!.Id)
                && addonName.Contains(x.Name, StringComparison.OrdinalIgnoreCase), ct);

            if (addon is null)
                return ($"I couldn't find an extra called '{addonName}'. Would you like to see other options?", [], null);

            lastItem.UnitPrice += addon.Price;
            slots["items"] = JsonSerializer.Serialize(cart);
            return ($"Added {addon.Name} (+{addon.Price:0.##}) to {lastItem.Name}.", [], null);
        }

        // ── Add item to cart ──────────────────────────────────────────────────
        if (TryExtractQuantityAndItem(original, out var qty, out _))
        {
            var match = menuItems.FirstOrDefault(i => original.Contains(i.Name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                var cart     = ParseCart(slots.GetValueOrDefault("items"));
                var existing = cart.FirstOrDefault(x => x.Name.Equals(match.Name, StringComparison.OrdinalIgnoreCase));
                if (existing is null) cart.Add(new CartItem { Name = match.Name, Quantity = qty, UnitPrice = match.BasePrice, Currency = match.Currency });
                else existing.Quantity += qty;
                slots["items"] = JsonSerializer.Serialize(cart);
                return ($"Added {qty}× {match.Name}. Anything else, or shall I confirm the order?", [], null);
            }
        }

        // ── Named item enquiry (no quantity) ─────────────────────────────────
        var menuMatch = menuItems.FirstOrDefault(i => lower.Contains(i.Name.ToLowerInvariant()));
        if (menuMatch is not null)
            return ($"{menuMatch.Name} is {menuMatch.BasePrice:0.##} {menuMatch.Currency}. How many would you like?", [], null);

        // ── Total ─────────────────────────────────────────────────────────────
        if (lower.Contains("total") || lower.Contains("how much"))
        {
            var cart = ParseCart(slots.GetValueOrDefault("items"));
            var fee  = slots.GetValueOrDefault("fulfillmentType") == "delivery" ? 3.99m : 0m;
            var tot  = cart.Sum(x => x.LineTotal) + fee;
            return cart.Count == 0
                ? ("Your cart is empty. What would you like to order?", [], null)
                : ($"Your current total is {tot:0.##} (including {fee:0.##} delivery fee).", [], null);
        }

        // ── Confirm / place order ─────────────────────────────────────────────
        if (lower.Contains("confirm") || lower.Contains("place the order") || lower.Contains("that's all"))
        {
            var cart = ParseCart(slots.GetValueOrDefault("items"));
            if (cart.Count == 0) return ("Your cart is empty. Please add some items first.", [], null);

            var subtotal = cart.Sum(x => x.LineTotal);
            var fee      = slots.GetValueOrDefault("fulfillmentType") == "delivery" ? 3.99m : 0m;
            var total    = subtotal + fee;
            var currency = cart.First().Currency;

            var order = new RestaurantOrder
            {
                Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
                CampaignId = session.CampaignId, CallSessionId = session.Id,
                FulfillmentType = slots.GetValueOrDefault("fulfillmentType") ?? "pickup",
                ItemsJson = JsonSerializer.Serialize(cart),
                Subtotal = subtotal, DeliveryFee = fee, Total = total, Currency = currency, Status = "Confirmed"
            };
            db.RestaurantOrders.Add(order);

            var final = new
            {
                type = "restaurant_order", orderId = order.Id,
                subtotal, deliveryFee = fee, total, currency,
                payment = slots.GetValueOrDefault("paymentMethod") ?? "unknown"
            };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            return ($"Order confirmed! Your total is {total:0.##} {currency}. Thank you, {slots.GetValueOrDefault("customerName", "there")}!", [], final);
        }

        return null;
    }

    private async Task<(string Reply, List<string> MissingSlots, object? FinalResult)?> HandleCourierExtrasAsync(
        CallSession session, CampaignConfiguration? config, string lower, Dictionary<string, string> slots, CancellationToken ct)
    {
        if (lower.Contains("confirm") && slots.ContainsKey("pickupAddress") && slots.ContainsKey("dropoffAddress") && slots.ContainsKey("weightKg"))
        {
            decimal.TryParse(slots.GetValueOrDefault("weightKg", "0"), out var weight);
            var profile = await db.CourierPricingProfiles.FirstOrDefaultAsync(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive, ct);
            if (profile is null) return ("I couldn't load pricing at this time. Please try again shortly.", [], null);

            var distKm = await ResolveDistanceKmAsync(slots.GetValueOrDefault("pickupAddress"), slots.GetValueOrDefault("dropoffAddress"), ct) ?? 8m;
            var urgencyFee = slots.GetValueOrDefault("urgency") == "same_day" ? 5m : 0m;
            var fragileFee = slots.GetValueOrDefault("packageType") == "fragile" ? 2m : 0m;
            var total = Math.Max(profile.MinimumFee, profile.BaseFee + profile.PricePerKm * distKm + profile.PricePerKg * weight + urgencyFee + fragileFee);

            var quote = new CourierQuote
            {
                Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
                CampaignId = session.CampaignId, CallSessionId = session.Id,
                PickupAddressJson = JsonSerializer.Serialize(new { address = slots.GetValueOrDefault("pickupAddress") }),
                DropoffAddressJson = JsonSerializer.Serialize(new { address = slots.GetValueOrDefault("dropoffAddress") }),
                DistanceKm = distKm, WeightKg = weight, PackageType = slots.GetValueOrDefault("packageType") ?? "standard",
                Urgency = slots.GetValueOrDefault("urgency") ?? "standard",
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(slots.GetValueOrDefault("urgency") == "same_day" ? 2 : 24),
                BaseFee = profile.BaseFee, DistanceFee = profile.PricePerKm * distKm, WeightFee = profile.PricePerKg * weight,
                UrgencyFee = urgencyFee + fragileFee, Total = total, Currency = profile.Currency, Status = "Quoted"
            };
            db.CourierQuotes.Add(quote);
            var final = new { type = "courier_order", quoteId = quote.Id, pickup = slots.GetValueOrDefault("pickupAddress"), dropoff = slots.GetValueOrDefault("dropoffAddress"), weightKg = weight, distanceKm = distKm, total, currency = profile.Currency };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            return ($"Confirmed! Your courier from {slots.GetValueOrDefault("pickupAddress")} to {slots.GetValueOrDefault("dropoffAddress")} is estimated at {total:0.##} {profile.Currency}. We'll be in touch, {slots.GetValueOrDefault("customerName", "there")}!", [], final);
        }
        return null;
    }

    private static (string Reply, List<string> MissingSlots, object? FinalResult)? HandleCabExtras(string lower, Dictionary<string, string> slots)
    {
        if (lower.Contains("helicopter"))
            return ("We don't offer that vehicle type. Available options are Standard, Executive, 6-Seater, and Wheelchair Accessible.", [], null);
        if (slots.TryGetValue("passengerCount", out var pcStr) && int.TryParse(pcStr, out var pc) && pc > 10)
            return ("That's more passengers than a single vehicle holds. Would you like me to arrange multiple vehicles?", [], null);
        if (lower.Contains("speak to someone") || lower.Contains("human") || lower.Contains("agent"))
            return ("I can connect you to the team. I've marked this call for handoff.", [], null);
        return null;
    }

    private static (string Reply, List<string> MissingSlots, object? FinalResult)? HandleDoctorExtras(
        string lower, string original, Dictionary<string, string> slots, CallSession session, CampaignConfiguration? config)
    {
        var highRisk = new[] { "chest pain", "cannot breathe", "severe bleeding", "unconscious", "suicidal" };
        if (highRisk.Any(k => lower.Contains(k)))
        {
            session.HandoffRequested = true;
            return ("That sounds like it needs urgent medical attention. Please contact emergency services or go to your nearest emergency department immediately.", [], null);
        }
        if (lower.Contains("medicine") || lower.Contains("diagnose") || lower.Contains("prescribe"))
            return ("I can help capture an appointment request, but I'm not able to give medical advice. Would you like to book an appointment?", [], null);

        var directory = TryParseDoctorDirectory(config?.ValidationRulesJson);
        if (directory is null || directory.Doctors.Count == 0) return null;

        // "Who are the doctors" / list request
        if (lower.Contains("which doctor") || lower.Contains("who are") || lower.Contains("list doctor") || lower.Contains("available doctor") || lower.Contains("any doctor"))
        {
            var doctorList = string.Join(", ", directory.Doctors.Select(d => $"{d.Name} ({d.Speciality})"));
            return ($"Our available doctors are: {doctorList}. Do you have a preference?", [], null);
        }

        // Try to extract preferredDoctor from message (name mention or "no preference")
        if (!slots.ContainsKey("preferredDoctor"))
        {
            var matched = directory.Doctors.FirstOrDefault(d =>
                lower.Contains(d.Name.ToLowerInvariant()) ||
                lower.Contains(d.Name.Split(' ').Last().ToLowerInvariant()));
            if (matched is not null)
                slots["preferredDoctor"] = matched.Name;
            else if (Regex.IsMatch(lower, @"\b(any|no preference|doesn'?t matter|don'?t mind|whoever|whoever is available)\b"))
                slots["preferredDoctor"] = "Any";
        }

        // Validate day-of-week availability when both doctor and datetime are known
        if (slots.TryGetValue("preferredDoctor", out var chosenDoctor) &&
            !string.Equals(chosenDoctor, "Any", StringComparison.OrdinalIgnoreCase) &&
            slots.TryGetValue("preferredDateTime", out var preferredDt) &&
            !string.IsNullOrWhiteSpace(preferredDt))
        {
            var day = ExtractDayOfWeek(preferredDt);
            if (day is not null)
            {
                var doctor = directory.Doctors.FirstOrDefault(d => d.Name.Equals(chosenDoctor, StringComparison.OrdinalIgnoreCase));
                if (doctor is not null && !doctor.AvailableDays.Contains(day, StringComparer.OrdinalIgnoreCase))
                {
                    slots.Remove("preferredDateTime");
                    var avail = string.Join(", ", doctor.AvailableDays);
                    return ($"I'm sorry, {chosenDoctor} is not available on {day}. They're available on {avail}. What day works for you?", [], null);
                }
            }
        }

        return null;
    }

    private static string? ExtractDayOfWeek(string text)
    {
        var lower = text.ToLowerInvariant();
        string[] days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
        return days.FirstOrDefault(d => lower.Contains(d.ToLowerInvariant()));
    }

    private static DoctorDirectory? TryParseDoctorDirectory(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<DoctorDirectory>(json, JsonOpts); }
        catch { return null; }
    }

    // ── Final result builder ──────────────────────────────────────────────────

    private static object BuildFinalResult(CampaignType type, Dictionary<string, string> slots, CallSession session)
    {
        return type switch
        {
            CampaignType.MedicareSales => new
            {
                type = "medicare_lead", leadName = slots.GetValueOrDefault("leadName"), phone = slots.GetValueOrDefault("phone"),
                ageRange = slots.GetValueOrDefault("ageRange"), currentCoverage = slots.GetValueOrDefault("currentCoverage"),
                state = slots.GetValueOrDefault("state"), callbackTime = slots.GetValueOrDefault("callbackTime"),
                interestLevel = slots.GetValueOrDefault("interestConfirmed", "Interested"), status = "CapturedOnly"
            },
            CampaignType.AcaSales => new
            {
                type = "aca_lead", firstName = slots.GetValueOrDefault("firstName"), phone = slots.GetValueOrDefault("phone"),
                state = slots.GetValueOrDefault("state"), currentInsuranceStatus = slots.GetValueOrDefault("currentInsuranceStatus"),
                householdSize = slots.GetValueOrDefault("householdSize"), incomeRange = slots.GetValueOrDefault("incomeRange"),
                coverageInterest = slots.GetValueOrDefault("coverageInterest"), tobaccoUse = slots.GetValueOrDefault("tobaccoUse"),
                callbackTime = slots.GetValueOrDefault("callbackTime"), status = "CapturedOnly"
            },
            CampaignType.FeSales => new
            {
                type = "fe_lead", firstName = slots.GetValueOrDefault("firstName"), age = slots.GetValueOrDefault("age"),
                phone = slots.GetValueOrDefault("phone"), state = slots.GetValueOrDefault("state"),
                tobaccoUse = slots.GetValueOrDefault("tobaccoUse"), healthConditions = slots.GetValueOrDefault("healthConditions"),
                coverageAmount = slots.GetValueOrDefault("coverageAmount"), beneficiaryName = slots.GetValueOrDefault("beneficiaryName"),
                callbackTime = slots.GetValueOrDefault("callbackTime"), status = "CapturedOnly"
            },
            CampaignType.DoctorAppointment => new
            {
                type = "doctor_appointment", patientName = slots.GetValueOrDefault("patientName"), phone = slots.GetValueOrDefault("phone"),
                reasonForVisit = slots.GetValueOrDefault("reasonForVisit"), preferredDateTime = slots.GetValueOrDefault("preferredDateTime"),
                preferredDoctor = slots.GetValueOrDefault("preferredDoctor"), branch = slots.GetValueOrDefault("branch"), status = "CapturedOnly"
            },
            CampaignType.CabBooking => new
            {
                type = "cab_booking", customerName = slots.GetValueOrDefault("customerName"), phone = slots.GetValueOrDefault("phone"),
                pickupLocation = slots.GetValueOrDefault("pickupLocation"), dropoffLocation = slots.GetValueOrDefault("dropoffLocation"),
                pickupDateTime = slots.GetValueOrDefault("pickupDateTime"), passengerCount = slots.GetValueOrDefault("passengerCount"),
                vehicleType = slots.GetValueOrDefault("vehicleType"), estimatedFare = "£18.00", status = "CapturedOnly"
            },
            _ => new { type = "lead", slots, status = "CapturedOnly" }
        };
    }

    private static string BuildConfirmation(CampaignType type, Dictionary<string, string> slots)
    {
        var name = slots.GetValueOrDefault("firstName") ?? slots.GetValueOrDefault("customerName") ?? slots.GetValueOrDefault("leadName") ?? slots.GetValueOrDefault("patientName") ?? "there";
        return type switch
        {
            CampaignType.MedicareSales    => $"Thank you, {name}! I've captured your details and a licensed Medicare specialist will call you {slots.GetValueOrDefault("callbackTime", "soon")}. Have a great day!",
            CampaignType.AcaSales         => $"Perfect, {name}! I've saved your information and a licensed health coverage agent will reach out {slots.GetValueOrDefault("callbackTime", "soon")} at {slots.GetValueOrDefault("phone", "the number you provided")}. Have a great day!",
            CampaignType.FeSales          => $"Thank you, {name}! A licensed final expense specialist will call you {slots.GetValueOrDefault("callbackTime", "soon")} at {slots.GetValueOrDefault("phone", "the number you provided")} to walk you through the options. Have a wonderful day!",
            CampaignType.DoctorAppointment=> $"Thank you, {name}. Your appointment request has been saved and the clinic team will confirm availability with you shortly.",
            CampaignType.CabBooking       => $"All set, {name}! Your cab booking from {slots.GetValueOrDefault("pickupLocation")} to {slots.GetValueOrDefault("dropoffLocation")} has been captured. We'll confirm it at {slots.GetValueOrDefault("phone")} shortly.",
            CampaignType.CourierService   => $"Booked, {name}! Your courier request has been submitted. We'll confirm pickup details at {slots.GetValueOrDefault("phone")} shortly.",
            _                             => $"Thank you, {name}! Everything has been saved and we'll be in touch soon."
        };
    }

    // ── Opt-out / objection intercept ─────────────────────────────────────────

    private static string? TryHandleOptOut(CallSession session, string lower, IAppDbContext db)
    {
        if (lower.Contains("remove me") || lower.Contains("stop calling") || lower.Contains("opt out") || lower.Contains("do not call"))
        {
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "lead_opt_out_requested" });
            return "Understood. I'll make sure your number is marked as do-not-contact. Sorry for any inconvenience. Have a good day!";
        }
        if (lower.Contains("not interested"))
            return "No problem at all. Thank you for your time. Have a great day!";
        if (lower.Contains("speak to a human") || lower.Contains("speak to someone") || lower.Contains("talk to an agent") || lower.Contains("transfer me"))
        {
            session.HandoffRequested = true;
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "handoff_requested" });
            return "Of course. I've flagged this call for a team member to follow up with you shortly.";
        }
        return null;
    }

    // ── Cross-campaign guard ──────────────────────────────────────────────────

    private static bool TryGetCrossCampaignRedirect(CampaignType type, string lower, out string reply)
    {
        reply = string.Empty;
        string[] restaurant = ["burger", "pizza", "menu", "deals", "order food"];
        string[] courier    = ["courier", "parcel", "delivery fee"];
        string[] cab        = ["book a cab", "book a taxi", "need a driver"];
        string[] doctor     = ["doctor", "appointment", "dr "];
        string[] medicare   = ["medicare", "part a", "part b"];
        string[] aca        = ["aca", "affordable care", "health subsidy"];
        string[] fe         = ["life insurance", "final expense", "funeral cover"];

        bool Has(string[] terms) => terms.Any(lower.Contains);

        switch (type)
        {
            case CampaignType.RestaurantOrder    when Has(courier) || Has(cab) || Has(doctor) || Has(medicare) || Has(aca) || Has(fe):
                reply = "I can help with restaurant orders here. What would you like to order?"; return true;
            case CampaignType.CourierService     when Has(restaurant) || Has(cab) || Has(doctor) || Has(medicare) || Has(aca) || Has(fe):
                reply = "I can help with courier bookings here. What is the pickup address?"; return true;
            case CampaignType.CabBooking         when Has(restaurant) || Has(courier) || Has(doctor) || Has(medicare) || Has(aca) || Has(fe):
                reply = "I can help with cab bookings here. Where should we pick you up from?"; return true;
            case CampaignType.DoctorAppointment  when Has(restaurant) || Has(courier) || Has(cab) || Has(medicare) || Has(aca) || Has(fe):
                reply = "I can help with clinic appointments here. What is the reason for your visit?"; return true;
            case CampaignType.MedicareSales      when Has(restaurant) || Has(courier) || Has(cab) || Has(doctor) || Has(aca) || Has(fe):
                reply = "I can only help with Medicare-related information in this call."; return true;
            case CampaignType.AcaSales           when Has(restaurant) || Has(courier) || Has(cab) || Has(doctor) || Has(medicare) || Has(fe):
                reply = "I can only help with health coverage options in this call."; return true;
            case CampaignType.FeSales            when Has(restaurant) || Has(courier) || Has(cab) || Has(doctor) || Has(medicare) || Has(aca):
                reply = "I can only help with final expense life insurance in this call."; return true;
            default: return false;
        }
    }

    // ── Questionnaire parser ──────────────────────────────────────────────────

    private static QuestionnaireDefinition TryParseQuestionnaire(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new QuestionnaireDefinition();
        try { return JsonSerializer.Deserialize<QuestionnaireDefinition>(json, JsonOpts) ?? new QuestionnaireDefinition(); }
        catch { return new QuestionnaireDefinition(); }
    }

    private static Dictionary<string, string> ParseSlots(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOpts) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
        catch { return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
    }

    // ── Restaurant cart helpers ───────────────────────────────────────────────

    private static List<CartItem> ParseCart(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<CartItem>>(json, JsonOpts) ?? []; }
        catch { return []; }
    }

    private static bool TryExtractQuantityAndItem(string input, out int quantity, out string item)
    {
        var words = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
            ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10
        };
        var wm = Regex.Match(input, @"\b(?<qty>one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<item>[a-zA-Z][a-zA-Z\s]+)", RegexOptions.IgnoreCase);
        if (wm.Success && words.TryGetValue(wm.Groups["qty"].Value, out quantity))
        {
            item = wm.Groups["item"].Value.Trim();
            return true;
        }
        var nm = Regex.Match(input, @"\b(?<qty>\d+)\s+(?<item>[a-zA-Z][a-zA-Z\s]+)", RegexOptions.IgnoreCase);
        quantity = 0; item = string.Empty;
        if (!nm.Success) return false;
        quantity = int.Parse(nm.Groups["qty"].Value);
        item = nm.Groups["item"].Value.Trim();
        return true;
    }

    // ── Courier distance helper ───────────────────────────────────────────────

    private async Task<decimal?> ResolveDistanceKmAsync(string? pickup, string? dropoff, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pickup) || string.IsNullOrWhiteSpace(dropoff)) return null;
        var from = await geocodingProvider.GeocodeAsync(pickup, ct);
        var to   = await geocodingProvider.GeocodeAsync(dropoff, ct);
        if (from is null || to is null) return null;
        return await routingProvider.GetDistanceKmAsync(from.Value, to.Value, ct);
    }

    // ── RAG helper ────────────────────────────────────────────────────────────

    private async Task<string?> TryGetRagScopedReplyAsync(CallSession session, CampaignConfiguration? config, string message, CancellationToken ct)
    {
        if (config is null || string.IsNullOrWhiteSpace(config.RagSettingsJson)) return null;
        RagRuntimeConfiguration? runtime;
        try { runtime = JsonSerializer.Deserialize<RagRuntimeConfiguration>(config.RagSettingsJson, JsonOpts); }
        catch { return null; }
        if (runtime is null || !runtime.Enabled || runtime.KnowledgeBaseId == Guid.Empty) return null;
        var result = await ragRetrievalService.SearchAsync(
            new RagSearchRequest(new RagScope(session.TenantId, session.ClientId, session.CampaignId, runtime.KnowledgeBaseId), message, runtime.TopK, runtime.MinScore, runtime.AllowedDocumentTypes ?? []), ct);
        if (!result.Found || result.Chunks.Count == 0) return null;
        return result.Chunks.OrderByDescending(x => x.Score).First().ChunkText;
    }

    // ── US states lookup ──────────────────────────────────────────────────────

    private static readonly (string Name, string Abbrev)[] States =
    [
        ("alabama","AL"),("alaska","AK"),("arizona","AZ"),("arkansas","AR"),("california","CA"),
        ("colorado","CO"),("connecticut","CT"),("delaware","DE"),("florida","FL"),("georgia","GA"),
        ("hawaii","HI"),("idaho","ID"),("illinois","IL"),("indiana","IN"),("iowa","IA"),
        ("kansas","KS"),("kentucky","KY"),("louisiana","LA"),("maine","ME"),("maryland","MD"),
        ("massachusetts","MA"),("michigan","MI"),("minnesota","MN"),("mississippi","MS"),("missouri","MO"),
        ("montana","MT"),("nebraska","NE"),("nevada","NV"),("new hampshire","NH"),("new jersey","NJ"),
        ("new mexico","NM"),("new york","NY"),("north carolina","NC"),("north dakota","ND"),("ohio","OH"),
        ("oklahoma","OK"),("oregon","OR"),("pennsylvania","PA"),("rhode island","RI"),("south carolina","SC"),
        ("south dakota","SD"),("tennessee","TN"),("texas","TX"),("utah","UT"),("vermont","VT"),
        ("virginia","VA"),("washington","WA"),("west virginia","WV"),("wisconsin","WI"),("wyoming","WY")
    ];

    // ── Shared options ────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // ── Inner types ───────────────────────────────────────────────────────────

    private sealed class QuestionnaireDefinition
    {
        [JsonPropertyName("openingScript")] public string? OpeningScript { get; set; }
        [JsonPropertyName("questions")]     public List<QuestionDefinition> Questions { get; set; } = [];
    }

    private sealed class QuestionDefinition
    {
        [JsonPropertyName("id")]          public string Id { get; set; } = string.Empty;
        [JsonPropertyName("order")]       public int Order { get; set; }
        [JsonPropertyName("question")]    public string Question { get; set; } = string.Empty;
        [JsonPropertyName("required")]    public bool Required { get; set; } = true;
        [JsonPropertyName("validValues")] public List<string>? ValidValues { get; set; }
    }

    private sealed class CartItem
    {
        [JsonPropertyName("name")]      public string Name { get; set; } = string.Empty;
        [JsonPropertyName("quantity")]  public int Quantity { get; set; }
        [JsonPropertyName("unitPrice")] public decimal UnitPrice { get; set; }
        [JsonPropertyName("currency")]  public string Currency { get; set; } = "USD";
        public decimal LineTotal => Quantity * UnitPrice;
    }

    private sealed class RagRuntimeConfiguration
    {
        public bool Enabled { get; set; }
        public Guid KnowledgeBaseId { get; set; }
        public int TopK { get; set; } = 4;
        public decimal MinScore { get; set; } = 0.72m;
        public List<string>? AllowedDocumentTypes { get; set; }
    }

    private sealed class DoctorDirectory
    {
        [JsonPropertyName("doctors")]          public List<DoctorInfo> Doctors { get; set; } = [];
        [JsonPropertyName("appointmentTypes")] public List<string> AppointmentTypes { get; set; } = [];
    }

    private sealed class DoctorInfo
    {
        [JsonPropertyName("name")]          public string Name { get; set; } = string.Empty;
        [JsonPropertyName("speciality")]    public string Speciality { get; set; } = string.Empty;
        [JsonPropertyName("availableDays")] public List<string> AvailableDays { get; set; } = [];
    }
}
