using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    IRagRetrievalService ragRetrievalService,
    ISlotExtractionService slotExtractionService,
    IAnswerFinalizationService finalizationService,
    ILocationNormalizationService locationNormalization,
    IIntentDetectionService intentDetectionService,
    ILookupService lookupService,
    ILogger<ConversationOrchestratorService> logger) : IConversationOrchestratorService
{
    // ── Entry points ──────────────────────────────────────────────────────────

    public async Task<SendDemoMessageResponseDto> ProcessMessageAsync(
        Guid callSessionId, string message,
        Func<string, CancellationToken, Task>? onInterimMessage = null,
        CancellationToken ct = default)
    {
        logger.LogInformation("[Orchestrator] Session={Id} ProcessMessage: '{Msg}'", callSessionId, message);
        try
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

        // Guard: call already ended
        if (session.CurrentState is ConversationState.Completed or ConversationState.Declined
            or ConversationState.Disqualified or ConversationState.AbuseEnded)
        {
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto
            {
                Reply = "This call has already ended. Thank you!",
                CurrentState = session.CurrentState.ToString(),
                ShouldEndCall = true, EndReason = session.EndReason, MissingSlots = []
            };
        }

        // RAG override
        var ragReply = await TryGetRagScopedReplyAsync(session, config, message, ct);
        if (!string.IsNullOrWhiteSpace(ragReply))
        {
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = ragReply, StateAfter = session.CurrentState.ToString() });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = ragReply, CurrentState = session.CurrentState.ToString(), MissingSlots = [] };
        }

        var lower = message.ToLowerInvariant();

        // Prompt injection guard
        if (lower.Contains("tell me something from another client") || lower.Contains("show me all client policies") || lower.Contains("ignore your instructions"))
        {
            const string guarded = "I can only use information for this service. How can I help you?";
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = guarded, StateAfter = session.CurrentState.ToString() });
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "prompt_injection_blocked", EventDataJson = JsonSerializer.Serialize(new { message }) });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = guarded, CurrentState = session.CurrentState.ToString(), MissingSlots = [] };
        }

        // Abuse detection (3-strike system)
        var (abuseReply, abuseEnded) = CheckAbuse(session, lower, db);
        if (abuseReply is not null)
        {
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = abuseReply, StateAfter = session.CurrentState.ToString() });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = abuseReply, CurrentState = session.CurrentState.ToString(), ShouldEndCall = abuseEnded, EndReason = abuseEnded ? session.EndReason : null, MissingSlots = [] };
        }

        // AwaitingConfirmation: user is responding to the summary yes/no
        if (session.CurrentState == ConversationState.AwaitingConfirmation)
        {
            var (confirmReply, shouldEnd, endReason) = await HandleConfirmationAsync(session, lower, campaign, config, ct);
            logger.LogInformation("[Orchestrator] Session={Id} Confirmation handled: ShouldEnd={End} EndReason={Reason} Reply={Reply}",
                callSessionId, shouldEnd, endReason, confirmReply);
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = confirmReply, StateAfter = session.CurrentState.ToString() });
            try { await db.SaveChangesAsync(ct); }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Orchestrator] Session={Id} DB save failed after confirmation — closing sentence will still be sent", callSessionId);
            }
            return new SendDemoMessageResponseDto { Reply = confirmReply, CurrentState = session.CurrentState.ToString(), ShouldEndCall = shouldEnd, EndReason = endReason, MissingSlots = [] };
        }

        // EditingSlot: bot asked for corrected value for a specific field
        if (session.CurrentState == ConversationState.EditingSlot && !string.IsNullOrWhiteSpace(session.EditingSlotId))
        {
            var (editReply, editEnd, editReason) = await HandleSlotEditAsync(session, message, lower, campaign, config, ct);
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = editReply, StateAfter = session.CurrentState.ToString() });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = editReply, CurrentState = session.CurrentState.ToString(), ShouldEndCall = editEnd, EndReason = editReason, MissingSlots = [] };
        }

        // Opt-out / not-interested intercept
        var optOutReply = TryHandleOptOut(session, lower, db);
        if (optOutReply is not null)
        {
            session.CurrentState = ConversationState.Declined;
            session.EndReason = "user_opt_out";
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = optOutReply, StateAfter = session.CurrentState.ToString() });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = optOutReply, CurrentState = session.CurrentState.ToString(), ShouldEndCall = true, EndReason = session.EndReason, MissingSlots = [] };
        }

        // Cross-campaign guard
        if (TryGetCrossCampaignRedirect(campaign.CampaignType, lower, out var redirect))
        {
            db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = redirect, StateAfter = session.CurrentState.ToString() });
            await db.SaveChangesAsync(ct);
            return new SendDemoMessageResponseDto { Reply = redirect, CurrentState = session.CurrentState.ToString(), MissingSlots = [] };
        }

        // Intent detection (multi-intent campaigns)
        if (session.CurrentState is ConversationState.Greeting or ConversationState.IntentDetection)
        {
            var rootQ = TryParseQuestionnaire(config?.QuestionnaireJson);
            if (rootQ.IsMultiIntent)
            {
                var (intentReply, intentEnd, intentReason) = await HandleIntentDetectionAsync(session, campaign, config, rootQ, message, lower, onInterimMessage, ct);
                db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = intentReply, StateAfter = session.CurrentState.ToString() });
                await db.SaveChangesAsync(ct);
                return new SendDemoMessageResponseDto { Reply = intentReply, CurrentState = session.CurrentState.ToString(), ShouldEndCall = intentEnd, EndReason = intentReason, MissingSlots = [] };
            }
        }

        // Main questionnaire engine
        var result = await HandleQuestionnaireAsync(session, campaign, config, message, lower, onInterimMessage, ct);
        db.CallTurns.Add(new CallTurn { Id = Guid.NewGuid(), CallSessionId = session.Id, TurnNumber = turnNumber + 1, Speaker = "bot", Text = result.Reply, StateAfter = session.CurrentState.ToString() });
        await db.SaveChangesAsync(ct);

        return new SendDemoMessageResponseDto
        {
            Reply = result.Reply, CurrentState = session.CurrentState.ToString(),
            MissingSlots = result.MissingSlots, FinalResult = result.FinalResult,
            ShouldEndCall = result.ShouldEndCall, EndReason = result.EndReason
        };

        } // end try
        catch (Exception ex)
        {
            logger.LogError(ex, "[Orchestrator] Session={Id} Unhandled exception in ProcessMessageAsync", callSessionId);
            throw;
        }
    }

    public Task<string> OrchestrateAsync(Guid callSessionId, string message, CancellationToken ct = default)
        => ProcessMessageAsync(callSessionId, message, ct: ct).ContinueWith(t => t.Result.Reply, ct);

    // ── Slot labels and keyword map (for confirmation edit flow) ─────────────

    private static readonly Dictionary<string, string> SlotLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["firstName"]             = "first name",
        ["customerName"]          = "name",
        ["leadName"]              = "name",
        ["patientName"]           = "name",
        ["beneficiaryName"]       = "beneficiary name",
        ["phone"]                 = "phone number",
        ["callbackPhone"]         = "callback phone",
        ["age"]                   = "age",
        ["ageRange"]              = "age range",
        ["state"]                 = "state",
        ["householdSize"]         = "household size",
        ["passengerCount"]        = "passenger count",
        ["incomeRange"]           = "income range",
        ["monthlyRevenueRange"]   = "monthly revenue",
        ["coverageInterest"]      = "coverage interest",
        ["coverageAmount"]        = "coverage amount",
        ["currentCoverage"]       = "current coverage",
        ["tobaccoUse"]            = "tobacco use",
        ["healthConditions"]      = "health conditions",
        ["currentInsuranceStatus"]= "insurance status",
        ["interestConfirmed"]     = "interest",
        ["callbackTime"]          = "callback time",
        ["fulfillmentType"]       = "fulfillment type",
        ["paymentMethod"]         = "payment method",
        ["urgency"]               = "urgency",
        ["packageType"]           = "package type",
        ["weightKg"]              = "package weight (kg)",
        ["vehicleType"]           = "vehicle type",
        ["pickupLocation"]        = "pickup location",
        ["dropoffLocation"]       = "dropoff location",
        ["pickupAddress"]         = "pickup address",
        ["dropoffAddress"]        = "dropoff address",
        ["pickupDateTime"]        = "pickup time",
        ["preferredDateTime"]     = "preferred date and time",
        ["preferredDoctor"]       = "preferred doctor",
        ["reasonForVisit"]        = "reason for visit",
        ["branch"]                = "clinic location",
        ["planType"]              = "plan type",
        // multi-intent service slots
        ["contactName"]           = "contact name",
        ["issueDescription"]      = "issue description",
        ["complaintDetail"]       = "complaint details",
        ["changeRequest"]         = "change request",
        ["inquiryText"]           = "inquiry",
        ["trackingNumber"]        = "tracking number",
        ["bookingRef"]            = "booking reference",
        ["orderRef"]              = "order reference",
        ["appointmentRef"]        = "appointment reference",
        ["specialty"]             = "specialty",
        ["newDate"]               = "new date",
        ["newDateTime"]           = "new date and time",
        ["itemDescription"]       = "item description",
        ["preferredDate"]         = "preferred date",
    };

    // ordered most-specific first so short keywords don't shadow longer ones
    private static readonly (string Keyword, string SlotId)[] SlotKeywords =
    [
        ("beneficiary name", "beneficiaryName"),
        ("beneficiary", "beneficiaryName"),
        ("callback phone", "callbackPhone"),
        ("callback time", "callbackTime"),
        ("callback", "callbackTime"),
        ("clinic location", "branch"),
        ("clinic branch", "branch"),
        ("clinic", "branch"),
        ("branch", "branch"),
        ("coverage amount", "coverageAmount"),
        ("coverage interest", "coverageInterest"),
        ("current coverage", "currentCoverage"),
        ("coverage", "coverageInterest"),
        ("dropoff address", "dropoffAddress"),
        ("dropoff location", "dropoffLocation"),
        ("dropoff", "dropoffLocation"),
        ("fulfillment", "fulfillmentType"),
        ("health condition", "healthConditions"),
        ("health", "healthConditions"),
        ("household size", "householdSize"),
        ("household", "householdSize"),
        ("income", "incomeRange"),
        ("insurance status", "currentInsuranceStatus"),
        ("current insurance", "currentInsuranceStatus"),
        ("insurance", "currentInsuranceStatus"),
        ("package weight", "weightKg"),
        ("weight", "weightKg"),
        ("passenger count", "passengerCount"),
        ("passengers", "passengerCount"),
        ("payment", "paymentMethod"),
        ("phone number", "phone"),
        ("phone", "phone"),
        ("pickup address", "pickupAddress"),
        ("pickup location", "pickupLocation"),
        ("pickup time", "pickupDateTime"),
        ("pickup", "pickupLocation"),
        ("plan type", "planType"),
        ("plan", "planType"),
        ("preferred doctor", "preferredDoctor"),
        ("preferred time", "preferredDateTime"),
        ("reason for visit", "reasonForVisit"),
        ("reason", "reasonForVisit"),
        ("state", "state"),
        ("tobacco", "tobaccoUse"),
        ("vehicle type", "vehicleType"),
        ("vehicle", "vehicleType"),
        ("age range", "ageRange"),
        ("age", "age"),
        // multi-intent service slot keywords
        ("tracking number", "trackingNumber"),
        ("tracking", "trackingNumber"),
        ("booking reference", "bookingRef"),
        ("booking ref", "bookingRef"),
        ("order reference", "orderRef"),
        ("order ref", "orderRef"),
        ("appointment reference", "appointmentRef"),
        ("appointment ref", "appointmentRef"),
        ("specialty", "specialty"),
        ("new date and time", "newDateTime"),
        ("new date", "newDate"),
        ("new time", "newDateTime"),
        ("preferred date", "preferredDate"),
        ("item description", "itemDescription"),
        ("lost item", "itemDescription"),
        ("complaint detail", "complaintDetail"),
        ("complaint", "complaintDetail"),
        ("issue description", "issueDescription"),
        ("issue", "issueDescription"),
        ("change request", "changeRequest"),
        ("inquiry", "inquiryText"),
        ("contact name", "contactName"),
        // name variants — each campaign uses one; TryParseFieldReference checks filledSlotIds
        ("patient name", "patientName"),
        ("customer name", "customerName"),
        ("lead name", "leadName"),
        ("my name", "firstName"),
        ("name", "contactName"),
        ("name", "leadName"),
        ("name", "customerName"),
        ("name", "patientName"),
        ("name", "firstName"),
    ];

    private static readonly HashSet<string> CorrectionIndicators = new(StringComparer.OrdinalIgnoreCase)
    {
        "wrong", "incorrect", "mistake", "error", "not right", "not correct",
        "actually", "no it's", "no it is", "i said", "i meant", "i mean",
        "should be", "it should", "change", "update", "correct it", "fix"
    };

    // ── Confirmation handler ──────────────────────────────────────────────────

    private async Task<(string Reply, bool ShouldEndCall, string? EndReason)> HandleConfirmationAsync(
        CallSession session, string lower, Campaign campaign, CampaignConfiguration? config, CancellationToken ct)
    {
        var questionnaire = GetEffectiveQuestionnaire(config, session.DetectedIntent);
        var slots = ParseSlots(session.CollectedSlotsJson);

        // Lookup-with-continuation offer: user is responding yes/no to "Would you like to book?"
        if (slots.TryGetValue("__lookup_offered__", out var offeredContinueIntent))
        {
            var offerYes = Regex.IsMatch(lower, @"\b(yes|yeah|yep|yup|sure|ok|okay|go ahead|book it|proceed|absolutely|definitely)\b");
            var offerNo  = Regex.IsMatch(lower, @"\b(no|nope|nah|not|no thanks|no thank)\b");
            if (offerYes && !offerNo)
            {
                slots.Remove("__lookup_offered__");
                session.DetectedIntent = offeredContinueIntent;
                session.CollectedSlotsJson = JsonSerializer.Serialize(slots);

                var root = TryParseQuestionnaire(config?.QuestionnaireJson);
                var bookingIntent = root.Intents.FirstOrDefault(i => i.Id == offeredContinueIntent);
                var bookingQ = bookingIntent?.Questionnaire ?? new QuestionnaireDefinition();

                // Skip already-filled slots (e.g. pickupLocation/dropoffLocation carried over from fare_estimate)
                var firstUnanswered = FindFirstUnansweredQuestion(bookingQ, slots);
                if (firstUnanswered is not null)
                {
                    session.CurrentState = ConversationState.CollectingSlots;
                    session.CurrentQuestionId = firstUnanswered.Id;
                    return (firstUnanswered.Question, false, null);
                }
                // All booking questions already filled — go straight to summary
                var summaryResult = await BuildSummaryAndAwaitConfirmationAsync(session, campaign, config, slots, bookingQ, null, ct);
                return (summaryResult.Reply, summaryResult.ShouldEndCall, summaryResult.EndReason);
            }
            if (offerNo || !offerYes)
            {
                session.CurrentState = ConversationState.Declined;
                session.EndReason = "user_declined_continue";
                return ("No problem! Feel free to call back when you're ready. Have a great day!", true, "user_declined_continue");
            }
        }

        // Priority 1: inline correction ("my age is 40", "actually my name is Sarah")
        var inline = TryParseInlineCorrection(lower, slots, questionnaire);
        if (inline is not null)
        {
            slots[inline.Value.SlotId] = inline.Value.Value;
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            var summary = BuildNumberedSummary(slots, questionnaire);
            return ($"Got it! I've updated that. {summary} Does everything look correct now?", false, null);
        }

        // Priority 2: field reference only ("change my phone number", "the state is wrong")
        var fieldRef = TryParseFieldReference(lower, slots, questionnaire);
        if (fieldRef is not null)
        {
            session.EditingSlotId = fieldRef;
            session.CurrentState = ConversationState.EditingSlot;
            var label = SlotLabels.GetValueOrDefault(fieldRef, fieldRef);
            return ($"Sure! What is the correct {label}?", false, null);
        }

        // Priority 3: yes — confirmed
        var yes = Regex.IsMatch(lower, @"\b(yes|yeah|yep|yup|sure|correct|confirm|absolutely|definitely|proceed|go ahead|ok|okay|that'?s right|looks good|all good|perfect|great)\b");
        var no  = Regex.IsMatch(lower, @"\b(no|nope|nah)\b");

        if (yes && !no)
        {
            session.CurrentState = ConversationState.Completed;
            session.EndReason = "completed_happy_path";
            // Save campaign-specific entities (courier, cab, doctor, restaurant)
            await SaveCampaignEntityAsync(session, campaign, config, slots, ct);
            // Primary booking intents always get the dynamic confirmation (includes reference ID).
            // Complaint/incident intents use their static closingScript.
            var useDynamic = string.IsNullOrWhiteSpace(session.DetectedIntent)
                             || PrimaryBookingIntents.Contains(session.DetectedIntent);
            var closing = (!useDynamic && !string.IsNullOrWhiteSpace(questionnaire.ClosingScript))
                ? questionnaire.ClosingScript
                : BuildConfirmation(campaign.CampaignType, slots, session);
            return (closing, true, "completed_happy_path");
        }

        // Priority 4: plain "no" without a specific field → check for price decline first, then ask what to change
        if (no && !yes)
        {
            var isPriceDecline = Regex.IsMatch(lower,
                @"\b(too (much|expensive|high|costly|pricey|dear)|can'?t afford|not worth|overpriced|too far|not (ok|okay) with (the |that )?(price|fare|cost)|don'?t want (it|to proceed))\b");
            if (isPriceDecline)
            {
                session.CurrentState = ConversationState.Declined;
                session.EndReason = "price_declined";
                return ("No problem at all! Feel free to call back if you change your mind. Have a great day!", true, "price_declined");
            }
            var summary = BuildNumberedSummary(slots, questionnaire);
            return ($"No problem! {summary} Which item would you like to change? You can say the number or the field name.", false, null);
        }

        // Priority 5: ambiguous
        return ("I'm sorry, I didn't quite catch that. Could you say yes to confirm, no to make a change, or tell me which detail to update?", false, null);
    }

    // ── EditingSlot handler ───────────────────────────────────────────────────

    private async Task<(string Reply, bool ShouldEndCall, string? EndReason)> HandleSlotEditAsync(
        CallSession session, string message, string lower,
        Campaign campaign, CampaignConfiguration? config, CancellationToken ct)
    {
        var slotId = session.EditingSlotId!;
        var questionnaire = GetEffectiveQuestionnaire(config, session.DetectedIntent);
        var slots = ParseSlots(session.CollectedSlotsJson);

        // Find the question for this slot to get validValues and question text
        var qDef = questionnaire.Questions.FirstOrDefault(q => (q.SlotId ?? q.Id) == slotId);
        var questionText = qDef?.Question ?? slotId;

        // Extract new value — regex first, then LLM (both carry type hint when available)
        var extracted = TryExtractValue(slotId, message, lower, qDef?.ValidValues, qDef?.SlotType);
        if (extracted is null)
            extracted = await slotExtractionService.ExtractAsync(slotId, questionText, message, qDef?.SlotType, ct);

        if (extracted is null)
        {
            var label = SlotLabels.GetValueOrDefault(slotId, slotId);
            return ($"I'm sorry, I didn't catch that. What is the correct {label}?", false, null);
        }

        // Disqualification check on new value
        var dq = CheckCampaignDisqualificationOnExtract(campaign.CampaignType, slotId, extracted, session);
        if (dq is not null)
            return (dq, true, session.EndReason);

        slots[slotId] = extracted;
        session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
        session.EditingSlotId = null;
        session.CurrentState = ConversationState.AwaitingConfirmation;

        var summary = BuildNumberedSummary(slots, questionnaire);
        return ($"Updated! {summary} Does everything look correct now?", false, null);
    }

    // ── Intent detection (multi-intent campaigns) ─────────────────────────────

    private async Task<(string Reply, bool ShouldEndCall, string? EndReason)> HandleIntentDetectionAsync(
        CallSession session, Campaign campaign, CampaignConfiguration? config,
        QuestionnaireDefinition rootQuestionnaire, string message, string lower,
        Func<string, CancellationToken, Task>? onInterimMessage,
        CancellationToken ct)
    {
        var triggers = rootQuestionnaire.Intents
            .Select(i => new IntentTrigger(i.Id, i.Name, i.Type, i.Triggers))
            .ToList();

        var match = await intentDetectionService.DetectAsync(message, triggers, ct);
        logger.LogInformation("[Orchestrator] Session={Id} Intent detection: message='{Msg}' matched={Intent}",
            session.Id, message, match?.IntentId ?? "null");

        if (match is null)
        {
            if (session.CurrentState == ConversationState.IntentDetection)
            {
                // Second failure → immediate human transfer
                session.CurrentState = ConversationState.Completed;
                session.EndReason = "human_transfer";
                session.HandoffRequested = true;
                return ("I'm sorry, I wasn't able to help with that. Let me connect you to a team member. Please stay on the line.", true, "human_transfer");
            }

            session.CurrentState = ConversationState.IntentDetection;
            var options = string.Join(", ", rootQuestionnaire.Intents
                .Where(i => i.Type != "transfer")
                .Select(i => i.Name));
            return ($"I can help with: {options}. Which would you like?", false, null);
        }

        var intent = rootQuestionnaire.Intents.First(i => i.Id == match.IntentId);
        session.DetectedIntent = intent.Id;
        logger.LogInformation("[Orchestrator] Session={Id} Intent set: id={IntentId} type={Type}", session.Id, intent.Id, intent.Type);

        // Transfer intents → immediate handoff
        if (intent.Type == "transfer")
        {
            session.CurrentState = ConversationState.Completed;
            session.EndReason = "human_transfer";
            session.HandoffRequested = true;
            var msg = !string.IsNullOrWhiteSpace(intent.TransferMessage)
                ? intent.TransferMessage
                : "Connecting you to a team member. Please stay on the line.";
            return (msg, true, "human_transfer");
        }

        // Lookup / collect intents → start the sub-questionnaire
        session.CurrentState = ConversationState.CollectingSlots;
        if (intent.Questionnaire is not null && intent.Questionnaire.Questions.Count > 0)
        {
            var firstQ = !string.IsNullOrWhiteSpace(intent.Questionnaire.StartQuestionId)
                ? intent.Questionnaire.Questions.FirstOrDefault(q => q.Id == intent.Questionnaire.StartQuestionId)
                : intent.Questionnaire.Questions.OrderBy(q => q.Order).FirstOrDefault();

            if (firstQ is not null)
            {
                session.CurrentQuestionId = firstQ.Id;
                return (firstQ.Question, false, null);
            }
        }

        // No sub-questionnaire (e.g. a collect intent that jumped straight to existing questions):
        // fall through to the main questionnaire engine by processing this message again.
        var result = await HandleQuestionnaireAsync(session, campaign, config, message, lower, onInterimMessage, ct);
        return (result.Reply, result.ShouldEndCall, result.EndReason);
    }

    // ── Inline correction / field reference parsers ───────────────────────────

    private static (string SlotId, string Value)? TryParseInlineCorrection(
        string lower, Dictionary<string, string> slots, QuestionnaireDefinition questionnaire)
    {
        // Must contain a correction indicator to avoid false positives during normal Q&A
        var hasIndicator = CorrectionIndicators.Any(ind => lower.Contains(ind));
        if (!hasIndicator) return null;

        var filledSlotIds = GetOrderedFilledSlots(slots, questionnaire);

        foreach (var (keyword, slotId) in SlotKeywords)
        {
            if (!filledSlotIds.Contains(slotId)) continue;
            if (!lower.Contains(keyword)) continue;

            // Try to extract a value for this slot from the correction message
            var extracted = TryExtractValue(slotId, lower, lower, null);
            if (extracted is not null && !CorrectionIndicators.Contains(extracted))
                return (slotId, extracted);
        }

        // Number-only inline corrections: "item 2 is 45" or "number 3, it's Texas"
        var numMatch = Regex.Match(lower, @"\b(?:item\s*|number\s*)?(?<n>[1-9])\b");
        if (numMatch.Success && int.TryParse(numMatch.Groups["n"].Value, out var idx))
        {
            var ordered = GetOrderedFilledSlots(slots, questionnaire);
            if (idx <= ordered.Count)
            {
                var targetSlot = ordered[idx - 1];
                var extracted = TryExtractValue(targetSlot, lower, lower, null);
                if (extracted is not null && !CorrectionIndicators.Contains(extracted))
                    return (targetSlot, extracted);
            }
        }

        return null;
    }

    private static string? TryParseFieldReference(string lower, Dictionary<string, string> slots, QuestionnaireDefinition questionnaire)
    {
        var filledSlotIds = GetOrderedFilledSlots(slots, questionnaire);

        // Number reference: "change number 2" or just "2"
        var numMatch = Regex.Match(lower, @"\b(?:number\s*|item\s*|#\s*)?(?<n>[1-9])\b");
        if (numMatch.Success && int.TryParse(numMatch.Groups["n"].Value, out var idx))
        {
            if (idx <= filledSlotIds.Count)
                return filledSlotIds[idx - 1];
        }

        // Keyword reference
        foreach (var (keyword, slotId) in SlotKeywords)
        {
            if (filledSlotIds.Contains(slotId) && lower.Contains(keyword))
                return slotId;
        }

        return null;
    }

    // ── Numbered summary builder ──────────────────────────────────────────────

    private static string BuildNumberedSummary(Dictionary<string, string> slots, QuestionnaireDefinition questionnaire)
    {
        var ordered = GetOrderedFilledSlots(slots, questionnaire);
        if (ordered.Count == 0) return "I don't have any details recorded yet.";

        var lines = ordered.Select((slotId, i) =>
        {
            var label = SlotLabels.GetValueOrDefault(slotId, slotId);
            var value = FormatSlotValue(slotId, slots[slotId]);
            return $"{i + 1}. {label}: {value}";
        });

        return "Here are the details I have: " + string.Join("; ", lines) + ".";
    }

    private static List<string> GetOrderedFilledSlots(Dictionary<string, string> slots, QuestionnaireDefinition questionnaire)
    {
        // items = cart blob; planType = derived; __ = internal flags (e.g. __finalized__)
        static bool IsDisplayable(string k) => k != "items" && k != "planType" && !k.StartsWith("__");

        var ordered = questionnaire.Questions
            .Select(q => q.SlotId ?? q.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(k => slots.ContainsKey(k) && IsDisplayable(k))
            .ToList();

        var extras = slots.Keys
            .Where(k => !ordered.Contains(k, StringComparer.OrdinalIgnoreCase) && IsDisplayable(k))
            .OrderBy(k => k);

        ordered.AddRange(extras);
        return ordered;
    }

    private static string FormatSlotValue(string slotId, string value)
    {
        return slotId switch
        {
            "tobaccoUse" or "healthConditions" or "interestConfirmed" or "currentInsuranceStatus"
                => value, // already "Yes"/"No"
            "coverageAmount" => value.StartsWith('$') ? value : $"${value}",
            "planType" => value switch
            {
                "simplified_issue" => "Simplified Issue",
                "graded_standard"  => "Graded Standard",
                "graded_benefit"   => "Graded Benefit",
                _ => value
            },
            _ => value
        };
    }

    // ── Abuse detection (3-strike) ────────────────────────────────────────────

    private static (string? Reply, bool CallEnded) CheckAbuse(CallSession session, string lower, IAppDbContext db)
    {
        if (!IsAbusive(lower)) return (null, false);

        session.AbuseWarningCount++;
        db.CallEvents.Add(new CallEvent
        {
            Id = Guid.NewGuid(), CallSessionId = session.Id,
            EventType = $"abuse_warning_{session.AbuseWarningCount}",
            EventDataJson = JsonSerializer.Serialize(new { count = session.AbuseWarningCount })
        });

        if (session.AbuseWarningCount >= 3)
        {
            session.CurrentState = ConversationState.AbuseEnded;
            session.EndReason = "abuse_policy_violation";
            return ("This call is being ended due to inappropriate language. Goodbye.", true);
        }

        return session.AbuseWarningCount == 2
            ? ("That's your second warning. Please keep this conversation respectful or this call will be ended. How can I assist you?", false)
            : ("I understand you may be frustrated, but please keep this conversation respectful. How can I assist you?", false);
    }

    private static bool IsAbusive(string lower)
    {
        string[] terms = ["fuck", "shit", "asshole", "bitch", "cunt", "bastard", "motherfucker", "damn you", "stupid ass", "moron"];
        return terms.Any(lower.Contains);
    }

    // ── Questionnaire engine (graph navigation) ───────────────────────────────

    private async Task<(string Reply, List<string> MissingSlots, object? FinalResult, bool ShouldEndCall, string? EndReason)> HandleQuestionnaireAsync(
        CallSession session, Campaign campaign, CampaignConfiguration? config,
        string message, string lower,
        Func<string, CancellationToken, Task>? onInterimMessage,
        CancellationToken ct)
    {
        var questionnaire = GetEffectiveQuestionnaire(config, session.DetectedIntent);
        var slots = ParseSlots(session.CollectedSlotsJson);

        // Campaign-specific extras first (cart, pricing, availability)
        var extra = await HandleCampaignSpecificAsync(session, campaign, config, message, lower, slots, ct);
        if (extra is not null)
        {
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            if (extra.Value.FinalResult is not null)
            {
                session.CurrentState = ConversationState.Completed;
                session.EndReason = "completed_happy_path";
                session.FinalResultJson = JsonSerializer.Serialize(extra.Value.FinalResult);
                return (extra.Value.Reply, [], extra.Value.FinalResult, true, "completed_happy_path");
            }
            return (extra.Value.Reply, extra.Value.MissingSlots, null, false, null);
        }

        session.CurrentState = ConversationState.CollectingSlots;

        // Initialise graph position
        if (string.IsNullOrWhiteSpace(session.CurrentQuestionId))
        {
            var firstQ = !string.IsNullOrWhiteSpace(questionnaire.StartQuestionId)
                ? questionnaire.Questions.FirstOrDefault(q => q.Id == questionnaire.StartQuestionId)
                : questionnaire.Questions.OrderBy(q => q.Order).FirstOrDefault();

            if (firstQ is null)
                return ("I'm ready to help. What can I do for you?", [], null, false, null);
            session.CurrentQuestionId = firstQ.Id;
        }

        var current = questionnaire.Questions.FirstOrDefault(q => q.Id == session.CurrentQuestionId);

        // Navigated past the last question — build summary
        if (current is null)
            return await BuildSummaryAndAwaitConfirmationAsync(session, campaign, config, slots, questionnaire, onInterimMessage, ct);

        // Resolve actual slot key (slotId overrides question id for shared-slot questions like healthConditions variants)
        var slotKey = current.SlotId ?? current.Id;

        // Extract: regex first (with type hint), then LLM fallback (with same type hint)
        var extracted = TryExtractValue(slotKey, message, lower, current.ValidValues, current.SlotType);
        logger.LogInformation("[Orchestrator] Session={Id} SlotExtract slot={Slot} regex={RegexResult}",
            session.Id, slotKey, extracted ?? "null");
        if (extracted is null)
        {
            extracted = await slotExtractionService.ExtractAsync(slotKey, current.Question, message, current.SlotType, ct);
            logger.LogInformation("[Orchestrator] Session={Id} SlotExtract slot={Slot} llm={LlmResult}",
                session.Id, slotKey, extracted ?? "null");
        }

        if (extracted is null)
        {
            // Text-based disqualification even without a cleanly extracted value
            var dqText = CheckCampaignDisqualification(campaign.CampaignType, slotKey, lower, session);
            if (dqText is not null)
                return (dqText, [], null, true, session.EndReason);

            var tried = IsMeaningfulResponse(lower);
            var prompt = tried ? $"I'm sorry, I didn't quite catch that. {current.Question}" : current.Question;
            var stillMissing = questionnaire.Questions.Where(q => q.Required && !slots.ContainsKey(q.SlotId ?? q.Id)).Select(q => q.SlotId ?? q.Id).ToList();
            return (prompt, stillMissing, null, false, null);
        }

        // Store under the resolved slot key
        slots[slotKey] = extracted;
        session.CollectedSlotsJson = JsonSerializer.Serialize(slots);

        // Value-based disqualification
        var dqExtract = CheckCampaignDisqualificationOnExtract(campaign.CampaignType, slotKey, extracted, session);
        if (dqExtract is not null)
            return (dqExtract, [], null, true, session.EndReason);

        // Branch resolution
        var branch = ResolveBranch(current, extracted);

        if (branch?.SetSlots is not null)
        {
            foreach (var kv in branch.SetSlots) slots[kv.Key] = kv.Value;
            session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
        }

        if (branch?.Action == "graceful_close")
        {
            session.CurrentState = ConversationState.Declined;
            session.EndReason = "user_not_interested";
            return ("No problem at all. Thank you for your time today. Have a wonderful day!", [], null, true, session.EndReason);
        }

        if (branch?.Action == "disqualify")
        {
            session.CurrentState = ConversationState.Disqualified;
            session.EndReason = "not_qualified";
            return ("Based on the information provided, it looks like you may not qualify for this program at this time. Thank you for your interest and have a great day!", [], null, true, session.EndReason);
        }

        // Advance — skip questions whose slots are already filled.
        // This handles the finalization re-ask case: only the flagged question was
        // removed; all downstream questions are still answered, so we jump straight
        // to the summary rather than re-asking them.
        var nextId = branch?.NextQuestionId ?? current.NextQuestionId;
        while (true)
        {
            if (string.IsNullOrWhiteSpace(nextId))
                return await BuildSummaryAndAwaitConfirmationAsync(session, campaign, config, slots, questionnaire, onInterimMessage, ct);

            var nextQ = questionnaire.Questions.FirstOrDefault(q => q.Id == nextId);
            if (nextQ is null)
                return await BuildSummaryAndAwaitConfirmationAsync(session, campaign, config, slots, questionnaire, onInterimMessage, ct);

            var nextSlotKey = nextQ.SlotId ?? nextQ.Id;
            if (!slots.ContainsKey(nextSlotKey))
            {
                // This slot is not yet answered — ask the question normally
                session.CurrentQuestionId = nextQ.Id;
                var missing = questionnaire.Questions
                    .Where(q => q.Required && !slots.ContainsKey(q.SlotId ?? q.Id))
                    .Select(q => q.SlotId ?? q.Id)
                    .ToList();
                return (nextQ.Question, missing, null, false, null);
            }

            // Slot already answered — resolve its branch with the stored value and skip past it
            var filledBranch = ResolveBranch(nextQ, slots[nextSlotKey]);
            nextId = filledBranch?.NextQuestionId ?? nextQ.NextQuestionId;
        }
    }

    private static QuestionDefinition? FindFirstUnansweredQuestion(QuestionnaireDefinition questionnaire, Dictionary<string, string> slots)
    {
        var current = !string.IsNullOrWhiteSpace(questionnaire.StartQuestionId)
            ? questionnaire.Questions.FirstOrDefault(q => q.Id == questionnaire.StartQuestionId)
            : questionnaire.Questions.OrderBy(q => q.Order).FirstOrDefault();

        while (current is not null)
        {
            var slotKey = current.SlotId ?? current.Id;
            if (!slots.ContainsKey(slotKey)) return current;
            var branch = ResolveBranch(current, slots[slotKey]);
            var nextId = branch?.NextQuestionId ?? current.NextQuestionId;
            if (string.IsNullOrWhiteSpace(nextId)) return null;
            current = questionnaire.Questions.FirstOrDefault(q => q.Id == nextId);
        }
        return null;
    }

    private static QuestionBranch? ResolveBranch(QuestionDefinition q, string value)
    {
        var exact = q.Branches.FirstOrDefault(b => string.Equals(b.When, value, StringComparison.OrdinalIgnoreCase));
        return exact ?? q.Branches.FirstOrDefault(b => b.When == "*");
    }

    private async Task<(string Reply, List<string> MissingSlots, object? FinalResult, bool ShouldEndCall, string? EndReason)>
        BuildSummaryAndAwaitConfirmationAsync(
            CallSession session, Campaign campaign, CampaignConfiguration? config,
            Dictionary<string, string> slots, QuestionnaireDefinition questionnaire,
            Func<string, CancellationToken, Task>? onInterimMessage,
            CancellationToken ct)
    {
        // ── Lookup intent path ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(session.DetectedIntent))
        {
            var root = TryParseQuestionnaire(config?.QuestionnaireJson);
            var intentDef = root.Intents.FirstOrDefault(i => i.Id == session.DetectedIntent);
            if (intentDef?.Type == "lookup")
            {
                if (onInterimMessage is not null)
                    await onInterimMessage("Please bear with me while I look that up.", ct);

                // Pre-calculate fare for fare_estimate so MockLookupService can use real distance/fare
                if (session.DetectedIntent == "fare_estimate" && !slots.ContainsKey("distanceKm"))
                {
                    var distKm = await ResolveDistanceKmAsync(
                        slots.GetValueOrDefault("pickupLocation"),
                        slots.GetValueOrDefault("dropoffLocation"), ct) ?? 5m;
                    slots["distanceKm"] = distKm.ToString("F1");
                    var fareSettings = ParseCabFareSettings(config?.ValidationRulesJson);
                    var pickDt = slots.GetValueOrDefault("pickupDateTime", "");
                    var airport = IsAirportAddress(slots.GetValueOrDefault("pickupLocation")) || IsAirportAddress(slots.GetValueOrDefault("dropoffLocation"));
                    slots["estimatedFare"] = CalculateCabFare(fareSettings, distKm, pickDt, airport).ToString("F2");
                    session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
                }

                var lookupResult = await lookupService.ExecuteAsync(session.DetectedIntent, slots,
                    new LookupContext(session.TenantId, session.ClientId, session.CampaignId, session.Id), ct);

                if (lookupResult.OffersContinue && !string.IsNullOrWhiteSpace(lookupResult.ContinueToIntentId))
                {
                    // Stay active so user can say yes/no to the continuation offer
                    slots["__lookup_offered__"] = lookupResult.ContinueToIntentId;
                    session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
                    session.CurrentState = ConversationState.AwaitingConfirmation;
                    return (lookupResult.Message, [], null, false, null);
                }

                session.CurrentState = ConversationState.Completed;
                session.EndReason = "lookup_completed";
                return (lookupResult.Message, [], null, true, "lookup_completed");
            }
        }

        // ── LLM Finalization ──────────────────────────────────────────────────
        // Run once after all slots collected. If the LLM detects an ambiguous or
        // invalid answer (e.g. "On" stored as a date), remove it and re-ask.
        if (!slots.ContainsKey("__finalized__") && questionnaire.Questions.Count > 0)
        {
            var answers = questionnaire.Questions
                .Select(q =>
                {
                    var key = q.SlotId ?? q.Id;
                    return slots.TryGetValue(key, out var ans) ? new SlotAnswer(key, q.Question, ans, q.SlotType) : null;
                })
                .OfType<SlotAnswer>()
                .ToList();

            if (answers.Count > 0)
            {
                if (onInterimMessage is not null)
                    await onInterimMessage("Please bear with me while I compile your information.", ct);

                var finalization = await finalizationService.FinalizeAnswersAsync(answers, ct);
                logger.LogInformation("[Orchestrator] Session={Id} Finalization: allClear={AllClear} ambiguous={Ambiguous}",
                    session.Id, finalization.AllClear, string.Join(",", finalization.AmbiguousSlotIds));

                // Always mark finalized now so we never re-run finalization on subsequent turns
                slots["__finalized__"] = "true";
                session.CollectedSlotsJson = JsonSerializer.Serialize(slots);

                if (!finalization.AllClear && finalization.AmbiguousSlotIds.Count > 0)
                {
                    var ambiguousSlotId = finalization.AmbiguousSlotIds[0];
                    var ambiguousQ = questionnaire.Questions
                        .FirstOrDefault(q => string.Equals(q.SlotId ?? q.Id, ambiguousSlotId, StringComparison.OrdinalIgnoreCase));

                    if (ambiguousQ is not null)
                    {
                        slots.Remove(ambiguousSlotId);
                        session.CurrentQuestionId = ambiguousQ.Id;
                        session.CurrentState = ConversationState.CollectingSlots;
                        session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
                        return ($"Just to confirm — {ambiguousQ.Question}", [], null, false, null);
                    }
                }
            }
            else
            {
                slots["__finalized__"] = "true";
                session.CollectedSlotsJson = JsonSerializer.Serialize(slots);
            }
        }

        session.CurrentState = ConversationState.AwaitingConfirmation;
        session.CurrentQuestionId = null;

        // Courier: proactive fare quote instead of generic numbered summary
        if (campaign.CampaignType == CampaignType.CourierService)
        {
            if (onInterimMessage is not null)
                await onInterimMessage("Please bear with me while I calculate your quote.", ct);
            var courierReply = await BuildCourierQuoteSummaryAsync(session, slots, ct);
            logger.LogInformation("[Orchestrator] Session={Id} CourierQuote built: {Reply}", session.Id, courierReply);
            var courierResult = BuildFinalResult(campaign.CampaignType, slots, session);
            session.FinalResultJson = JsonSerializer.Serialize(courierResult);
            return (courierReply, [], courierResult, false, null);
        }

        // Cab: proactive fare quote instead of generic numbered summary
        if (campaign.CampaignType == CampaignType.CabBooking)
        {
            if (onInterimMessage is not null)
                await onInterimMessage("Please bear with me while I calculate your fare.", ct);
            var cabReply = await BuildCabQuoteSummaryAsync(session, config, slots, ct);
            logger.LogInformation("[Orchestrator] Session={Id} CabQuote built: {Reply}", session.Id, cabReply);
            var cabResult = BuildFinalResult(campaign.CampaignType, slots, session);
            session.FinalResultJson = JsonSerializer.Serialize(cabResult);
            return (cabReply, [], cabResult, false, null);
        }

        var finalResult = BuildFinalResult(campaign.CampaignType, slots, session);
        session.FinalResultJson = JsonSerializer.Serialize(finalResult);
        var summary = BuildNumberedSummary(slots, questionnaire);
        return ($"{summary} Does everything look correct?", [], finalResult, false, null);
    }

    private async Task<string> BuildCourierQuoteSummaryAsync(CallSession session, Dictionary<string, string> slots, CancellationToken ct)
    {
        var profile = await db.CourierPricingProfiles
            .FirstOrDefaultAsync(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive, ct);
        if (profile is null)
            return "Your courier booking details are ready. Shall I go ahead and confirm the booking?";

        decimal distKm;
        if (!slots.TryGetValue("distanceKm", out var storedDist) || !decimal.TryParse(storedDist, out distKm))
        {
            distKm = await ResolveDistanceKmAsync(slots.GetValueOrDefault("pickupAddress"), slots.GetValueOrDefault("dropoffAddress"), ct) ?? 8m;
            slots["distanceKm"] = distKm.ToString("F1");
        }

        decimal.TryParse(slots.GetValueOrDefault("weightKg", "0"), out var weight);
        var packageType = slots.GetValueOrDefault("packageType", "standard");
        var urgency     = slots.GetValueOrDefault("urgency", "standard");

        var fare = await CalculateCourierFareAsync(profile, distKm, weight, packageType, urgency, ct);
        slots["estimatedFare"]     = fare.ToString("F2");
        slots["estimatedCurrency"] = profile.Currency;

        var pickup  = slots.GetValueOrDefault("pickupAddress", "pickup");
        var dropoff = slots.GetValueOrDefault("dropoffAddress", "destination");

        return $"That's approximately {distKm:F1} km. Estimated cost for a {weight:F1} kg {packageType} package from {pickup} to {dropoff}: {profile.Currency}{fare:F2}. Are you happy with that price and shall I go ahead and confirm the booking?";
    }

    private async Task<string> BuildCabQuoteSummaryAsync(CallSession session, CampaignConfiguration? config, Dictionary<string, string> slots, CancellationToken ct)
    {
        var settings = ParseCabFareSettings(config?.ValidationRulesJson);

        decimal distKm;
        if (!slots.TryGetValue("distanceKm", out var storedDist) || !decimal.TryParse(storedDist, out distKm))
        {
            distKm = await ResolveDistanceKmAsync(slots.GetValueOrDefault("pickupLocation"), slots.GetValueOrDefault("dropoffLocation"), ct) ?? 5m;
            slots["distanceKm"] = distKm.ToString("F1");
        }

        var pickup         = slots.GetValueOrDefault("pickupLocation", "pickup");
        var dropoff        = slots.GetValueOrDefault("dropoffLocation", "destination");
        var pickupDateTime = slots.GetValueOrDefault("pickupDateTime", "");
        var isAirport      = IsAirportAddress(pickup) || IsAirportAddress(dropoff);
        var isNight        = IsNightTime(pickupDateTime);

        var fare = CalculateCabFare(settings, distKm, pickupDateTime, isAirport);
        slots["estimatedFare"]     = fare.ToString("F2");
        slots["estimatedCurrency"] = "GBP";

        var vehicleType = slots.GetValueOrDefault("vehicleType", "Standard");
        var surchargeNote = isAirport ? " (includes airport fee)" : (isNight ? " (includes night surcharge)" : "");

        return $"Your {vehicleType} from {pickup} to {dropoff} is approximately {distKm:F1} km. Estimated fare: £{fare:F2}{surchargeNote}. Are you happy with that fare and shall I confirm your booking?";
    }

    private static string? CheckCampaignDisqualification(CampaignType type, string slotId, string lower, CallSession session)
    {
        if (type == CampaignType.MedicareSales && slotId == "ageRange" &&
            Regex.IsMatch(lower, @"\b(under 65|younger|not yet 65|below 65)\b"))
        {
            session.CurrentState = ConversationState.Disqualified;
            session.EndReason = "age_not_eligible_medicare";
            return "Medicare is primarily available to those 65 and older. Unfortunately you may not qualify at this time. Thank you for your time and have a great day!";
        }
        return null;
    }

    private static string? CheckCampaignDisqualificationOnExtract(CampaignType type, string slotId, string value, CallSession session)
    {
        if (type == CampaignType.FeSales && slotId == "age" &&
            int.TryParse(value, out var age) && (age < 50 || age > 85))
        {
            session.CurrentState = ConversationState.Disqualified;
            session.EndReason = "age_out_of_range_fe";
            return $"Our final expense program covers individuals between 50 and 85 years old. At {age}, you may not qualify for this specific plan at this time. Thank you for your interest and have a great day!";
        }
        return null;
    }

    // Slots whose content is structurally unambiguous — safe to scan across all turns
    private static bool IsUniquelyTypedSlot(string id) => id is
        "phone" or "callbackPhone" or "age" or "state" or
        "tobaccoUse" or "healthConditions" or "interestConfirmed" or
        "fulfillmentType" or "paymentMethod" or "urgency" or "packageType" or "vehicleType";

    private static string? TryExtractValue(string slotId, string message, string lower, List<string>? validValues, string? slotType = null)
    {
        return slotId switch
        {
            "items" => null,

            "firstName" or "customerName" or "leadName" or "patientName" or "beneficiaryName" or "contactName"
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
                => lower.Contains("family") ? "Family"
                 : lower.Contains("individual") || lower.Contains("single") || lower.Contains("just me") || lower.Contains("myself") ? "Individual"
                 : Regex.IsMatch(lower, @"\b(no|not|none|don'?t|not interested|neither)\b") ? "None"
                 : null,

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
                 : Regex.IsMatch(lower, @"\bunder\b") ? "under 65"
                 : null,

            "coverageAmount"
                => ExtractCoverageAmount(lower),

            "incomeRange" or "monthlyRevenueRange"
                => ExtractIncomeRange(message, lower),

            // Location slots: strip spoken sentence prefixes before storing
            "pickupLocation" or "dropoffLocation" or "pickupAddress" or "dropoffAddress"
                => StripLocationPrefix(message),

            // Reference slots: extract only the alphanumeric code, not the surrounding sentence
            "bookingRef" or "trackingNumber" or "orderRef" or "appointmentRef"
                => ExtractReferenceCode(message),

            // Date/time slots require an actual recognisable date or time pattern
            "pickupDateTime" or "preferredDateTime" or "DateTime"
                => ExtractDateTimeValue(message, lower),

            _ => ExtractBySlotType(slotType, message, lower)
        };
    }

    // Strips spoken sentence prefixes from location answers:
    //   "From Rawalpindi, Pakistan."                      → "Rawalpindi, Pakistan"
    //   "I'm heading toward the airport."                 → "the airport"
    //   "Going to London Heathrow"                        → "London Heathrow"
    //   "It will be picked up from, Heathrow Airport."   → "Heathrow Airport"
    //   "Picked up from Victoria Station"                 → "Victoria Station"
    //   "Departing from Manchester Piccadilly"            → "Manchester Piccadilly"
    private static string? StripLocationPrefix(string message)
    {
        const string pattern =
            @"^(?:" +
            // First-person + movement verb: "I'm going to...", "I am heading from..."
            @"i(?:'m|\s+am)\s+(?:going|heading|coming|travelling|traveling)(?:\s+(?:to|from|toward|towards))?\s+|" +
            // Movement verb at start: "Going to...", "Heading toward..."
            @"(?:going|heading|travelling|traveling)\s+(?:to|from|toward|towards)\s+|" +
            // Simple preposition: "From X" / "To X"
            @"(?:from|to)\s+|" +
            // Passive pickup (long form): "it will/'ll be picked up from, X" / "it'll be collected from X"
            @"it(?:\s+will|\s+'ll)?\s+be\s+(?:picked\s+up|collected)\s+from[,\s]+|" +
            // Passive pickup (short): "picked up from X" / "collected from X"
            @"(?:picked\s+up|collected)\s+from[,\s]+|" +
            // Implicit source: "it's from X" / "it is from X"
            @"it(?:'s|\s+is)\s+from\s+|" +
            // Nominal pickup/dropoff: "the pickup is from/at X" / "my pickup will be at X"
            @"(?:the|my)\s+(?:pickup|pick.?up|collection|dropoff|drop.?off)\s+(?:is|will\s+be)\s+(?:from|at)\s+|" +
            // Departure verbs: "departing from X" / "leaving from X" / "starting from X"
            @"(?:departing|leaving|starting)\s+(?:from|at)\s+" +
            @")";

        var trimmed = message.Trim();
        var stripped = Regex.Replace(trimmed, pattern, string.Empty, RegexOptions.IgnoreCase).TrimEnd('.');

        // Safety net: if the regex found nothing to strip and the reply is long enough
        // to be a full sentence (6+ words), return null so the LLM extractor handles it.
        if (string.Equals(stripped, trimmed.TrimEnd('.'), StringComparison.OrdinalIgnoreCase)
            && stripped.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 5)
            return null;

        return IsMeaningfulResponse(stripped.ToLowerInvariant()) ? stripped : null;
    }

    // Extracts an alphanumeric reference code from a spoken reply.
    // Scans tokens for the first that contains BOTH letters and digits (min 3 chars),
    // returning it uppercased. Returns null when no code-like token is found so the
    // LLM extractor handles unusual phrasing (e.g. purely numeric IDs).
    //   "Yes. It is 6B7D855D."      → "6B7D855D"
    //   "My reference is BOOK-24-1" → "BOOK-24-1"
    //   "6b7d855d"                  → "6B7D855D"
    private static string? ExtractReferenceCode(string message)
    {
        foreach (Match m in Regex.Matches(message, @"\b([A-Z0-9]{3,}(?:[-][A-Z0-9]{2,})*)\b", RegexOptions.IgnoreCase))
        {
            var candidate = m.Groups[1].Value.ToUpperInvariant();
            if (candidate.Any(char.IsLetter) && candidate.Any(char.IsDigit))
                return candidate;
        }
        return null;
    }

    // Falls through from TryExtractValue when no slot-specific handler matches.
    // Applies type-constraint validation instead of blindly accepting any non-empty string.
    private static string? ExtractBySlotType(string? slotType, string message, string lower) =>
        slotType switch
        {
            "date" or "datetime" => ExtractDateTimeValue(message, lower),
            "number"             => ExtractNumber(lower),
            _                    => IsMeaningfulResponse(lower) ? message.Trim() : null
        };

    // Validates that a date/time slot contains an actual date/time expression.
    // Rejects bare prepositions like "On" or "At" that the LLM occasionally returns.
    private static string? ExtractDateTimeValue(string message, string lower)
    {
        // Relative dates
        if (Regex.IsMatch(lower, @"\b(today|tomorrow|tonight|yesterday)\b")) return message.Trim();
        // "next/this <day-or-period>"
        if (Regex.IsMatch(lower, @"\b(next|this)\s+(monday|tuesday|wednesday|thursday|friday|saturday|sunday|week|weekend|month)\b")) return message.Trim();
        // Standalone day of week
        if (Regex.IsMatch(lower, @"\b(monday|tuesday|wednesday|thursday|friday|saturday|sunday)\b")) return message.Trim();
        // Month name present
        if (Regex.IsMatch(lower, @"\b(january|february|march|april|may|june|july|august|september|october|november|december)\b")) return message.Trim();
        // Ordinal day number (1st, 2nd, 3rd, 15th)
        if (Regex.IsMatch(lower, @"\b\d{1,2}(st|nd|rd|th)\b")) return message.Trim();
        // Numeric date (3/15, 15/3, 2024-03-15)
        if (Regex.IsMatch(lower, @"\b\d{1,2}[\/\-]\d{1,2}(?:[\/\-]\d{2,4})?\b")) return message.Trim();
        // Explicit time with am/pm
        if (Regex.IsMatch(lower, @"\b\d{1,2}(?::\d{2})?\s*(am|pm)\b")) return message.Trim();
        // 24-hour time (HH:MM)
        if (Regex.IsMatch(lower, @"\b\d{1,2}:\d{2}\b")) return message.Trim();
        // Named time-of-day periods
        if (Regex.IsMatch(lower, @"\b(morning|afternoon|evening|night|midnight|noon|midday)\b")) return message.Trim();
        // Bare prepositions ("on", "at", "in") or other non-date words → reject
        return null;
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
            @"^(?<n>[A-Z][a-z]{1,15}(?:\s+[A-Z][a-z]{1,20}){0,2})[.,!?]?$",
        };
        foreach (var pat in patterns)
        {
            var m = Regex.Match(message.Trim(), pat, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var candidate = m.Groups["n"].Value.Trim().TrimEnd('.');
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
        // Word-to-number before digit regex so "four" is always caught
        var wordMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
            ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
            ["eleven"] = 11, ["twelve"] = 12, ["thirteen"] = 13, ["fourteen"] = 14, ["fifteen"] = 15
        };
        foreach (var (word, num) in wordMap)
        {
            if (Regex.IsMatch(lower, $@"\b{Regex.Escape(word)}\b"))
                return num.ToString();
        }
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

    private static readonly HashSet<string> PrimaryBookingIntents =
        new(StringComparer.OrdinalIgnoreCase) { "book_cab", "book_pickup", "new_order", "book_appointment" };

    private async Task<(string Reply, List<string> MissingSlots, object? FinalResult)?> HandleCampaignSpecificAsync(
        CallSession session, Campaign campaign, CampaignConfiguration? config,
        string message, string lower, Dictionary<string, string> slots, CancellationToken ct)
    {
        // Skip cart / pricing extras for non-booking intents (complaint, lookup, transfer, etc.)
        if (!string.IsNullOrWhiteSpace(session.DetectedIntent) &&
            !PrimaryBookingIntents.Contains(session.DetectedIntent))
            return null;

        return campaign.CampaignType switch
        {
            CampaignType.RestaurantOrder   => await HandleRestaurantExtrasAsync(session, config, lower, message, slots, ct),
            CampaignType.CourierService    => await HandleCourierExtrasAsync(session, config, lower, slots, ct),
            CampaignType.CabBooking        => await HandleCabExtrasAsync(session, lower, slots),
            CampaignType.DoctorAppointment => HandleDoctorExtras(lower, message, slots, session, config),
            _ => null
        };
    }

    private async Task<(string Reply, List<string> MissingSlots, object? FinalResult)?> HandleRestaurantExtrasAsync(
        CallSession session, CampaignConfiguration? config, string lower, string original, Dictionary<string, string> slots, CancellationToken ct)
    {
        var settings = ParseRestaurantSettings(config?.ValidationRulesJson);
        var menuItems = await db.MenuItems
            .Where(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive && x.IsAvailable)
            .ToListAsync(ct);

        if (lower.Contains("menu") || lower.Contains("categories") || lower.Contains("what do you have") || lower.Contains("what's available"))
        {
            var cats = await db.MenuCategories
                .Where(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive)
                .OrderBy(x => x.SortOrder).Select(x => x.Name).ToListAsync(ct);
            return cats.Count == 0
                ? ("We don't have menu categories set up yet.", [], null)
                : ($"We have: {string.Join(", ", cats.Take(6))}. What would you like?", [], null);
        }

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

        var menuMatch = menuItems.FirstOrDefault(i => lower.Contains(i.Name.ToLowerInvariant()));
        if (menuMatch is not null)
            return ($"{menuMatch.Name} is {menuMatch.BasePrice:0.##} {menuMatch.Currency}. How many would you like?", [], null);

        if (lower.Contains("total") || lower.Contains("how much"))
        {
            var cart     = ParseCart(slots.GetValueOrDefault("items"));
            var subtotal = cart.Sum(x => x.LineTotal);
            var fee      = slots.GetValueOrDefault("fulfillmentType") == "delivery"
                ? (settings.FreeDeliveryThreshold > 0 && subtotal >= settings.FreeDeliveryThreshold ? 0m : settings.DeliveryFee)
                : 0m;
            var tax = subtotal * (settings.TaxRatePercent / 100m);
            var tot = subtotal + fee + tax;
            return cart.Count == 0
                ? ("Your cart is empty. What would you like to order?", [], null)
                : ($"Your current total is {tot:0.##} {settings.Currency} (subtotal {subtotal:0.##}, delivery {fee:0.##}, tax {tax:0.##}).", [], null);
        }

        if (lower.Contains("confirm") || lower.Contains("place the order") || lower.Contains("that's all"))
        {
            var cart = ParseCart(slots.GetValueOrDefault("items"));
            if (cart.Count == 0) return ("Your cart is empty. Please add some items first.", [], null);

            var subtotal = cart.Sum(x => x.LineTotal);
            var fee      = slots.GetValueOrDefault("fulfillmentType") == "delivery"
                ? (settings.FreeDeliveryThreshold > 0 && subtotal >= settings.FreeDeliveryThreshold ? 0m : settings.DeliveryFee)
                : 0m;
            var tax      = subtotal * (settings.TaxRatePercent / 100m);
            decimal.TryParse(slots.GetValueOrDefault("discount", "0"), out var discount);
            var total    = subtotal + fee + tax - discount;
            var currency = cart.First().Currency;

            var order = new RestaurantOrder
            {
                Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
                CampaignId = session.CampaignId, CallSessionId = session.Id,
                CustomerName    = slots.GetValueOrDefault("customerName", ""),
                Phone           = slots.GetValueOrDefault("phone", ""),
                FulfillmentType = slots.GetValueOrDefault("fulfillmentType") ?? "pickup",
                ItemsJson = JsonSerializer.Serialize(cart),
                Subtotal = subtotal, DeliveryFee = fee, Tax = tax, Discount = discount,
                Total = total, Currency = currency, Status = "Confirmed"
            };
            db.RestaurantOrders.Add(order);

            var final = new
            {
                type = "restaurant_order", orderId = order.Id,
                subtotal, deliveryFee = fee, tax, discount, total, currency,
                payment = slots.GetValueOrDefault("paymentMethod") ?? "unknown"
            };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            var cartLines = string.Join(", ", cart.Select(i => $"{i.Quantity}× {i.Name} {i.LineTotal:0.##} {currency}"));
            return ($"Order confirmed! {cartLines}. Total: {total:0.##} {currency}. Thank you, {slots.GetValueOrDefault("customerName", "there")}!", [], final);
        }

        return null;
    }

    private async Task<(string Reply, List<string> MissingSlots, object? FinalResult)?> HandleCourierExtrasAsync(
        CallSession session, CampaignConfiguration? config, string lower, Dictionary<string, string> slots, CancellationToken ct)
    {
        var hasAddresses = slots.ContainsKey("pickupAddress") && slots.ContainsKey("dropoffAddress");
        var hasWeight    = slots.ContainsKey("weightKg");
        var hasEstimate  = slots.ContainsKey("estimatedFare");

        // Early confirm shortcut — triggered before questionnaire completes (user says "confirm" mid-flow)
        var isAffirmation = lower.Contains("confirm") || Regex.IsMatch(lower, @"\b(yes|yeah|yep|yup|go ahead|book it|proceed)\b");
        if (isAffirmation && hasAddresses && hasWeight && hasEstimate)
        {
            var profile = await db.CourierPricingProfiles
                .FirstOrDefaultAsync(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive, ct);
            if (profile is null) return ("I couldn't load pricing. Please try again shortly.", [], null);

            decimal.TryParse(slots.GetValueOrDefault("distanceKm", "0"), out var storedDist);
            var distKm = storedDist > 0 ? storedDist
                : (await ResolveDistanceKmAsync(slots.GetValueOrDefault("pickupAddress"), slots.GetValueOrDefault("dropoffAddress"), ct) ?? 8m);

            decimal.TryParse(slots.GetValueOrDefault("weightKg", "0"), out var weight);
            var packageType = slots.GetValueOrDefault("packageType", "standard");
            var urgency     = slots.GetValueOrDefault("urgency", "standard");

            var total = await CalculateCourierFareAsync(profile, distKm, weight, packageType, urgency, ct);

            var quote = new CourierQuote
            {
                Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
                CampaignId = session.CampaignId, CallSessionId = session.Id,
                PickupAddressJson  = JsonSerializer.Serialize(new { address = slots.GetValueOrDefault("pickupAddress") }),
                DropoffAddressJson = JsonSerializer.Serialize(new { address = slots.GetValueOrDefault("dropoffAddress") }),
                DistanceKm = distKm, WeightKg = weight, PackageType = packageType, Urgency = urgency,
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(urgency == "same_day" ? 2 : 24),
                BaseFee = profile.BaseFee, DistanceFee = profile.PricePerKm * distKm,
                WeightFee = profile.PricePerKg * weight, UrgencyFee = 0m,
                Total = total, Currency = profile.Currency, Status = "Quoted"
            };
            db.CourierQuotes.Add(quote);

            var order = new CourierOrder
            {
                Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
                CampaignId = session.CampaignId, CallSessionId = session.Id, CourierQuoteId = quote.Id,
                CustomerName = slots.GetValueOrDefault("customerName", ""),
                Phone        = slots.GetValueOrDefault("phone", ""),
                FinalResultJson = "", Status = "Confirmed"
            };
            db.CourierOrders.Add(order);

            var final = new
            {
                type = "courier_order", quoteId = quote.Id, orderId = order.Id,
                pickup = slots.GetValueOrDefault("pickupAddress"), dropoff = slots.GetValueOrDefault("dropoffAddress"),
                weightKg = weight, distanceKm = distKm, total, currency = profile.Currency
            };
            session.FinalResultJson = JsonSerializer.Serialize(final);
            var name = slots.GetValueOrDefault("customerName", "there");
            return ($"Confirmed! Your courier from {slots.GetValueOrDefault("pickupAddress")} to {slots.GetValueOrDefault("dropoffAddress")} is estimated at {profile.Currency}{total:F2}. We'll be in touch, {name}!", [], final);
        }

        return null;
    }

    private static Task<(string Reply, List<string> MissingSlots, object? FinalResult)?> HandleCabExtrasAsync(
        CallSession session, string lower, Dictionary<string, string> slots)
    {
        if (lower.Contains("helicopter"))
            return Task.FromResult<(string, List<string>, object?)?>(("We don't offer helicopter transport. Available options are Standard, Executive, 6-Seater, and Wheelchair Accessible.", [], null));
        if (slots.TryGetValue("passengerCount", out var pcStr) && int.TryParse(pcStr, out var pc) && pc > 10)
            return Task.FromResult<(string, List<string>, object?)?>(("That's more passengers than a single vehicle holds. Would you like me to arrange multiple vehicles?", [], null));
        if (lower.Contains("speak to someone") || lower.Contains("human") || lower.Contains("agent"))
        {
            session.HandoffRequested = true;
            return Task.FromResult<(string, List<string>, object?)?>(("I can connect you to the team. I've marked this call for handoff.", [], null));
        }
        return Task.FromResult<(string, List<string>, object?)?>( null);
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

        if (lower.Contains("which doctor") || lower.Contains("who are") || lower.Contains("list doctor") || lower.Contains("available doctor") || lower.Contains("any doctor"))
        {
            var doctorList = string.Join(", ", directory.Doctors.Select(d => $"{d.Name} ({d.Speciality})"));
            return ($"Our available doctors are: {doctorList}. Do you have a preference?", [], null);
        }

        if (!slots.ContainsKey("preferredDoctor"))
        {
            var matched = directory.Doctors.FirstOrDefault(d =>
                lower.Contains(d.Name.ToLowerInvariant()) ||
                lower.Contains(d.Name.Split(' ').Last().ToLowerInvariant()));
            if (matched is not null)
                slots["preferredDoctor"] = matched.Name;
            else if (Regex.IsMatch(lower, @"\b(any|no preference|doesn'?t matter|don'?t mind|whoever)\b"))
                slots["preferredDoctor"] = "Any";
        }

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
                planType = slots.GetValueOrDefault("planType", "pending_review"),
                coverageAmount = slots.GetValueOrDefault("coverageAmount"), beneficiaryName = slots.GetValueOrDefault("beneficiaryName"),
                callbackTime = slots.GetValueOrDefault("callbackTime"), status = "CapturedOnly"
            },
            CampaignType.DoctorAppointment => new
            {
                type = "doctor_appointment", patientName = slots.GetValueOrDefault("patientName"), phone = slots.GetValueOrDefault("phone"),
                reasonForVisit = slots.GetValueOrDefault("reasonForVisit"), preferredDateTime = slots.GetValueOrDefault("preferredDateTime"),
                preferredDoctor = slots.GetValueOrDefault("preferredDoctor"), branch = slots.GetValueOrDefault("branch"),
                appointmentId = (string?)null, status = "CapturedOnly"
            },
            CampaignType.CabBooking => new
            {
                type = "cab_booking", customerName = slots.GetValueOrDefault("customerName"), phone = slots.GetValueOrDefault("phone"),
                pickupLocation = slots.GetValueOrDefault("pickupLocation"), dropoffLocation = slots.GetValueOrDefault("dropoffLocation"),
                pickupDateTime = slots.GetValueOrDefault("pickupDateTime"), passengerCount = slots.GetValueOrDefault("passengerCount"),
                vehicleType = slots.GetValueOrDefault("vehicleType"),
                estimatedFare = slots.TryGetValue("estimatedFare", out var ef) ? $"£{ef}" : "TBC",
                distanceKm = slots.GetValueOrDefault("distanceKm", ""), currency = "GBP", status = "CapturedOnly"
            },
            CampaignType.CourierService => new
            {
                type = "courier_booking", customerName = slots.GetValueOrDefault("customerName"), phone = slots.GetValueOrDefault("phone"),
                pickupAddress = slots.GetValueOrDefault("pickupAddress"), dropoffAddress = slots.GetValueOrDefault("dropoffAddress"),
                weightKg = slots.GetValueOrDefault("weightKg"), packageType = slots.GetValueOrDefault("packageType"),
                urgency = slots.GetValueOrDefault("urgency"),
                estimatedFare = slots.TryGetValue("estimatedFare", out var cef) ? cef : "TBC",
                currency = slots.GetValueOrDefault("estimatedCurrency", "GBP"),
                distanceKm = slots.GetValueOrDefault("distanceKm", ""), orderId = (string?)null, status = "CapturedOnly"
            },
            _ => new { type = "lead", slots, status = "CapturedOnly" }
        };
    }

    private static string BuildConfirmation(CampaignType type, Dictionary<string, string> slots, CallSession session)
    {
        var name  = slots.GetValueOrDefault("firstName") ?? slots.GetValueOrDefault("customerName")
                 ?? slots.GetValueOrDefault("leadName")  ?? slots.GetValueOrDefault("patientName") ?? "there";
        var phone = slots.GetValueOrDefault("phone", "the number you provided");
        var ref_  = TryExtractShortRef(session.FinalResultJson, type);
        return type switch
        {
            CampaignType.RestaurantOrder   => $"Your order has been placed{ref_}. Thank you, {name}! We'll contact you at {phone} if needed.",
            CampaignType.CabBooking        => $"All set, {name}! Your cab has been booked{ref_}. We'll confirm at {phone} shortly.",
            CampaignType.CourierService    => $"Booked, {name}! Your courier order{ref_} has been submitted. We'll confirm at {phone} shortly.",
            CampaignType.DoctorAppointment => $"Thank you, {name}. Your appointment{ref_} has been captured and our team will confirm at {phone} shortly.",
            CampaignType.MedicareSales     => $"Thank you, {name}! A licensed Medicare specialist will call you {slots.GetValueOrDefault("callbackTime", "soon")}. Have a great day!",
            CampaignType.AcaSales          => $"Perfect, {name}! A licensed health coverage agent will reach out {slots.GetValueOrDefault("callbackTime", "soon")} at {phone}. Have a great day!",
            CampaignType.FeSales           => $"Thank you, {name}! A licensed final expense specialist will call you {slots.GetValueOrDefault("callbackTime", "soon")} at {phone}. Have a wonderful day!",
            _                              => $"Thank you, {name}! Everything has been saved and we'll be in touch soon."
        };
    }

    private static string TryExtractShortRef(string? finalResultJson, CampaignType type)
    {
        if (string.IsNullOrWhiteSpace(finalResultJson)) return "";
        try
        {
            using var doc = JsonDocument.Parse(finalResultJson);
            var root = doc.RootElement;
            var key = type switch
            {
                CampaignType.RestaurantOrder   => "orderId",
                CampaignType.CabBooking        => "bookingId",
                CampaignType.CourierService    => "orderId",
                CampaignType.DoctorAppointment => "appointmentId",
                _ => null
            };
            if (key is not null && root.TryGetProperty(key, out var el) && Guid.TryParse(el.GetString(), out var guid))
                return $" (ref: {guid.ToString("N")[..8].ToUpper()})";
        }
        catch { }
        return "";
    }

    // ── Opt-out / objection intercept ─────────────────────────────────────────

    private static string? TryHandleOptOut(CallSession session, string lower, IAppDbContext db)
    {
        if (lower.Contains("remove me") || lower.Contains("stop calling") || lower.Contains("opt out") || lower.Contains("do not call"))
        {
            db.CallEvents.Add(new CallEvent { Id = Guid.NewGuid(), CallSessionId = session.Id, EventType = "lead_opt_out_requested" });
            return "Understood. I'll make sure your number is marked as do-not-contact. Sorry for any inconvenience. Have a good day!";
        }

        // Cancellation / disinterest phrases — cover service campaigns mid-booking as well
        if (lower.Contains("not interested")
            || lower.Contains("no thanks")
            || lower.Contains("no thank you")
            || lower.Contains("not today")
            || lower.Contains("not now")
            || lower.Contains("changed my mind")
            || lower.Contains("never mind")
            || lower.Contains("nevermind")
            || lower.Contains("forget it")
            || lower.Contains("cancel this")
            || lower.Contains("don't need this")
            || lower.Contains("don't want this")
            || lower.Contains("i don't need")
            || lower.Contains("i don't want")
            || Regex.IsMatch(lower, @"^(no\s*thanks|cancel|stop|quit|bye|goodbye|end)\s*[.!?]?$"))
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
            case CampaignType.RestaurantOrder   when Has(courier) || Has(cab) || Has(doctor) || Has(medicare) || Has(aca) || Has(fe):
                reply = "I can help with restaurant orders here. What would you like to order?"; return true;
            case CampaignType.CourierService    when Has(restaurant) || Has(cab) || Has(doctor) || Has(medicare) || Has(aca) || Has(fe):
                reply = "I can help with courier bookings here. What is the pickup address?"; return true;
            case CampaignType.CabBooking        when Has(restaurant) || Has(courier) || Has(doctor) || Has(medicare) || Has(aca) || Has(fe):
                reply = "I can help with cab bookings here. Where should we pick you up from?"; return true;
            case CampaignType.DoctorAppointment when Has(restaurant) || Has(courier) || Has(cab) || Has(medicare) || Has(aca) || Has(fe):
                reply = "I can help with clinic appointments here. What is the reason for your visit?"; return true;
            case CampaignType.MedicareSales     when Has(restaurant) || Has(courier) || Has(cab) || Has(doctor) || Has(aca) || Has(fe):
                reply = "I can only help with Medicare-related information in this call."; return true;
            case CampaignType.AcaSales          when Has(restaurant) || Has(courier) || Has(cab) || Has(doctor) || Has(medicare) || Has(fe):
                reply = "I can only help with health coverage options in this call."; return true;
            case CampaignType.FeSales           when Has(restaurant) || Has(courier) || Has(cab) || Has(doctor) || Has(medicare) || Has(aca):
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

    // Returns the sub-questionnaire for the active intent, or the root questionnaire for single-intent campaigns.
    private static QuestionnaireDefinition GetEffectiveQuestionnaire(CampaignConfiguration? config, string? detectedIntent)
    {
        var root = TryParseQuestionnaire(config?.QuestionnaireJson);
        if (string.IsNullOrWhiteSpace(detectedIntent) || !root.IsMultiIntent) return root;
        var intent = root.Intents.FirstOrDefault(i => i.Id == detectedIntent);
        return intent?.Questionnaire ?? root;
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

        // Convert messy speech → clean geocoding candidates (one LLM call).
        // Nominatim never receives raw spoken text; it only ever sees the LLM output.
        var (normalizedPickup, normalizedDropoff) = await locationNormalization.NormalizeLocationsAsync(pickup, dropoff, ct);

        var from = await geocodingProvider.GeocodeAsync(normalizedPickup, ct);
        var to   = await geocodingProvider.GeocodeAsync(normalizedDropoff, ct);
        if (from is null || to is null) return null;
        return await routingProvider.GetDistanceKmAsync(from.Value, to.Value, ct);
    }

    // ─�� Courier fare calculator (band pricing) ────────────────────────────────

    private async Task<decimal> CalculateCourierFareAsync(
        CourierPricingProfile profile, decimal distanceKm, decimal weightKg,
        string packageType, string urgency, CancellationToken ct)
    {
        var distBand = await db.CourierDistanceBands
            .Where(b => b.CourierPricingProfileId == profile.Id && b.FromKm <= distanceKm && b.ToKm > distanceKm)
            .OrderBy(b => b.FromKm).FirstOrDefaultAsync(ct);
        var distFee = distBand is not null ? distBand.Fee : profile.PricePerKm * distanceKm;

        var weightBand = await db.CourierWeightBands
            .Where(b => b.CourierPricingProfileId == profile.Id && b.FromKg <= weightKg && b.ToKg > weightKg)
            .OrderBy(b => b.FromKg).FirstOrDefaultAsync(ct);
        var weightFee = weightBand is not null ? weightBand.Fee : profile.PricePerKg * weightKg;

        var subtotal = profile.BaseFee + distFee + weightFee;

        if (!string.IsNullOrWhiteSpace(profile.SettingsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(profile.SettingsJson);
                var root = doc.RootElement;
                if (urgency == "same_day" && root.TryGetProperty("urgentMultiplier", out var multEl))
                    subtotal *= multEl.GetDecimal();
                if (packageType == "fragile" && root.TryGetProperty("fragilePackageExtraFee", out var fragEl))
                    subtotal += fragEl.GetDecimal();
            }
            catch { }
        }

        return Math.Max(profile.MinimumFee, subtotal);
    }

    // ── Cab fare calculator ───────────────────────────────────────────────────

    private static decimal CalculateCabFare(CabFareSettings settings, decimal distanceKm, string pickupDateTime, bool isAirportRide)
    {
        var fare = settings.BaseFare + settings.PricePerKm * distanceKm;
        if (isAirportRide) fare += settings.AirportPickupFee;
        if (IsNightTime(pickupDateTime)) fare *= settings.NightChargeMultiplier;
        return Math.Max(settings.MinimumFare, fare);
    }

    private static bool IsNightTime(string dateTimeStr)
    {
        if (string.IsNullOrWhiteSpace(dateTimeStr)) return false;
        var ampm = Regex.Match(dateTimeStr, @"\b(\d{1,2})(?::\d{2})?\s*(am|pm)\b", RegexOptions.IgnoreCase);
        if (ampm.Success)
        {
            var h = int.Parse(ampm.Groups[1].Value);
            var pm = ampm.Groups[2].Value.Equals("pm", StringComparison.OrdinalIgnoreCase);
            if (pm && h != 12) h += 12;
            else if (!pm && h == 12) h = 0;
            return h >= 22 || h < 6;
        }
        var h24 = Regex.Match(dateTimeStr, @"\b(\d{1,2}):(\d{2})\b");
        return h24.Success && int.TryParse(h24.Groups[1].Value, out var hour) && (hour >= 22 || hour < 6);
    }

    private static bool IsAirportAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return false;
        var l = address.ToLowerInvariant();
        return l.Contains("airport") || l.Contains("heathrow") || l.Contains("gatwick")
            || l.Contains("stansted") || l.Contains("luton") || l.Contains("city airport")
            || l.Contains("lhr") || l.Contains("lgw") || l.Contains("stn") || l.Contains("lut");
    }

    // ── Campaign entity persistence (called from HandleConfirmationAsync) ─────

    private async Task SaveCampaignEntityAsync(
        CallSession session, Campaign campaign, CampaignConfiguration? config,
        Dictionary<string, string> slots, CancellationToken ct)
    {
        // Non-primary collect intents go to a generic complaint/incident handler,
        // not to the primary booking entity (which would produce garbage records).
        if (session.DetectedIntent is "complaint" or "delivery_complaint" or "lost_item")
        {
            await SaveComplaintOrIncidentAsync(session, slots, ct);
            return;
        }

        switch (campaign.CampaignType)
        {
            case CampaignType.CourierService:
                await SaveCourierEntitiesAsync(session, slots, ct);
                break;
            case CampaignType.CabBooking:
                await SaveCabBookingAsync(session, slots, ct);
                break;
            case CampaignType.DoctorAppointment:
                await SaveDoctorAppointmentAsync(session, slots, ct);
                break;
            case CampaignType.RestaurantOrder:
                await SaveRestaurantOrderAsync(session, config, slots, ct);
                break;
        }
    }

    private Task SaveComplaintOrIncidentAsync(CallSession session, Dictionary<string, string> slots, CancellationToken ct)
    {
        var eventType = session.DetectedIntent switch
        {
            "complaint"          => "restaurant_complaint",
            "delivery_complaint" => "delivery_complaint",
            "lost_item"          => "lost_item_report",
            _                    => "complaint"
        };

        var cleanSlots = slots
            .Where(kv => !kv.Key.StartsWith("__"))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        db.CallEvents.Add(new CallEvent
        {
            Id            = Guid.NewGuid(),
            CallSessionId = session.Id,
            EventType     = eventType,
            EventDataJson = JsonSerializer.Serialize(new
            {
                intentId = session.DetectedIntent,
                slots    = cleanSlots,
                status   = "Captured"
            })
        });

        session.FinalResultJson = JsonSerializer.Serialize(new
        {
            type     = eventType,
            intentId = session.DetectedIntent,
            slots    = cleanSlots,
            status   = "Captured"
        });

        return Task.CompletedTask;
    }

    private async Task SaveCourierEntitiesAsync(CallSession session, Dictionary<string, string> slots, CancellationToken ct)
    {
        // Skip if the early-confirm path already saved (FinalResultJson contains orderId)
        if (!string.IsNullOrWhiteSpace(session.FinalResultJson) && session.FinalResultJson.Contains("\"orderId\""))
            return;

        var profile = await db.CourierPricingProfiles
            .FirstOrDefaultAsync(x => x.TenantId == session.TenantId && x.ClientId == session.ClientId && x.IsActive, ct);
        if (profile is null) return;

        decimal.TryParse(slots.GetValueOrDefault("distanceKm", "0"), out var distKm);
        if (distKm == 0)
            distKm = await ResolveDistanceKmAsync(slots.GetValueOrDefault("pickupAddress"), slots.GetValueOrDefault("dropoffAddress"), ct) ?? 8m;

        decimal.TryParse(slots.GetValueOrDefault("weightKg", "0"), out var weight);
        var packageType = slots.GetValueOrDefault("packageType", "standard");
        var urgency     = slots.GetValueOrDefault("urgency", "standard");

        var total = await CalculateCourierFareAsync(profile, distKm, weight, packageType, urgency, ct);

        var quote = new CourierQuote
        {
            Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
            CampaignId = session.CampaignId, CallSessionId = session.Id,
            PickupAddressJson  = JsonSerializer.Serialize(new { address = slots.GetValueOrDefault("pickupAddress") }),
            DropoffAddressJson = JsonSerializer.Serialize(new { address = slots.GetValueOrDefault("dropoffAddress") }),
            DistanceKm = distKm, WeightKg = weight, PackageType = packageType, Urgency = urgency,
            EstimatedDeliveryTime = DateTime.UtcNow.AddHours(urgency == "same_day" ? 2 : 24),
            BaseFee = profile.BaseFee, DistanceFee = profile.PricePerKm * distKm,
            WeightFee = profile.PricePerKg * weight, UrgencyFee = 0m,
            Total = total, Currency = profile.Currency, Status = "Quoted"
        };
        db.CourierQuotes.Add(quote);

        var order = new CourierOrder
        {
            Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
            CampaignId = session.CampaignId, CallSessionId = session.Id, CourierQuoteId = quote.Id,
            CustomerName = slots.GetValueOrDefault("customerName", ""),
            Phone        = slots.GetValueOrDefault("phone", ""),
            FinalResultJson = "", Status = "Confirmed"
        };
        db.CourierOrders.Add(order);

        session.FinalResultJson = JsonSerializer.Serialize(new
        {
            type = "courier_order", quoteId = quote.Id, orderId = order.Id,
            pickup = slots.GetValueOrDefault("pickupAddress"), dropoff = slots.GetValueOrDefault("dropoffAddress"),
            weightKg = weight, distanceKm = distKm, total, currency = profile.Currency
        });
    }

    private Task SaveCabBookingAsync(CallSession session, Dictionary<string, string> slots, CancellationToken ct)
    {
        decimal.TryParse(slots.GetValueOrDefault("distanceKm", "0"), out var distKm);
        decimal.TryParse(slots.GetValueOrDefault("estimatedFare", "0"), out var fare);
        int.TryParse(slots.GetValueOrDefault("passengerCount", "1"), out var pax);

        var pickup         = slots.GetValueOrDefault("pickupLocation", "");
        var dropoff        = slots.GetValueOrDefault("dropoffLocation", "");
        var pickupDateTime = slots.GetValueOrDefault("pickupDateTime", "");
        var isAirport      = IsAirportAddress(pickup) || IsAirportAddress(dropoff);

        var booking = new CabBooking
        {
            Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
            CampaignId = session.CampaignId, CallSessionId = session.Id,
            CustomerName = slots.GetValueOrDefault("customerName", ""),
            Phone        = slots.GetValueOrDefault("phone", ""),
            PickupLocation = pickup, DropoffLocation = dropoff, PickupDateTime = pickupDateTime,
            PassengerCount = pax, VehicleType = slots.GetValueOrDefault("vehicleType", "Standard"),
            DistanceKm = distKm, EstimatedFare = fare, Currency = "GBP",
            IsAirportPickup = isAirport, IsNightSurcharge = IsNightTime(pickupDateTime), Status = "Confirmed"
        };
        db.CabBookings.Add(booking);

        session.FinalResultJson = JsonSerializer.Serialize(new
        {
            type = "cab_booking", bookingId = booking.Id,
            customerName = booking.CustomerName, phone = booking.Phone,
            pickupLocation = pickup, dropoffLocation = dropoff, pickupDateTime,
            passengerCount = pax, vehicleType = booking.VehicleType,
            distanceKm = distKm, estimatedFare = fare, currency = "GBP",
            isAirportPickup = isAirport, isNightSurcharge = booking.IsNightSurcharge
        });
        return Task.CompletedTask;
    }

    private Task SaveDoctorAppointmentAsync(CallSession session, Dictionary<string, string> slots, CancellationToken ct)
    {
        var appointment = new DoctorAppointment
        {
            Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
            CampaignId = session.CampaignId, CallSessionId = session.Id,
            PatientName      = slots.GetValueOrDefault("patientName", ""),
            Phone            = slots.GetValueOrDefault("phone", ""),
            ReasonForVisit   = slots.GetValueOrDefault("reasonForVisit", ""),
            PreferredDateTime = slots.GetValueOrDefault("preferredDateTime", ""),
            PreferredDoctor  = slots.GetValueOrDefault("preferredDoctor", ""),
            ClinicBranch     = slots.GetValueOrDefault("branch", ""),
            Status = "Pending"
        };
        db.DoctorAppointments.Add(appointment);

        session.FinalResultJson = JsonSerializer.Serialize(new
        {
            type = "doctor_appointment", appointmentId = appointment.Id,
            patientName = appointment.PatientName, phone = appointment.Phone,
            reasonForVisit = appointment.ReasonForVisit, preferredDateTime = appointment.PreferredDateTime,
            preferredDoctor = appointment.PreferredDoctor, branch = appointment.ClinicBranch, status = "CapturedOnly"
        });
        return Task.CompletedTask;
    }

    private async Task SaveRestaurantOrderAsync(CallSession session, CampaignConfiguration? config, Dictionary<string, string> slots, CancellationToken ct)
    {
        // Skip if the direct "confirm" path already saved
        if (!string.IsNullOrWhiteSpace(session.FinalResultJson) && session.FinalResultJson.Contains("\"orderId\""))
            return;

        var cart = ParseCart(slots.GetValueOrDefault("items"));
        if (cart.Count == 0) return;

        var settings = ParseRestaurantSettings(config?.ValidationRulesJson);
        var subtotal  = cart.Sum(x => x.LineTotal);
        var fee       = slots.GetValueOrDefault("fulfillmentType") == "delivery"
            ? (settings.FreeDeliveryThreshold > 0 && subtotal >= settings.FreeDeliveryThreshold ? 0m : settings.DeliveryFee)
            : 0m;
        var tax = subtotal * (settings.TaxRatePercent / 100m);
        decimal.TryParse(slots.GetValueOrDefault("discount", "0"), out var discount);
        var total    = subtotal + fee + tax - discount;
        var currency = cart.FirstOrDefault()?.Currency ?? settings.Currency;

        var order = new RestaurantOrder
        {
            Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
            CampaignId = session.CampaignId, CallSessionId = session.Id,
            CustomerName    = slots.GetValueOrDefault("customerName", ""),
            Phone           = slots.GetValueOrDefault("phone", ""),
            FulfillmentType = slots.GetValueOrDefault("fulfillmentType", "pickup"),
            ItemsJson = JsonSerializer.Serialize(cart),
            Subtotal = subtotal, DeliveryFee = fee, Tax = tax, Discount = discount,
            Total = total, Currency = currency, Status = "Confirmed"
        };
        db.RestaurantOrders.Add(order);

        session.FinalResultJson = JsonSerializer.Serialize(new
        {
            type = "restaurant_order", orderId = order.Id,
            subtotal, deliveryFee = fee, tax, discount, total, currency,
            payment = slots.GetValueOrDefault("paymentMethod") ?? "unknown"
        });

        await Task.CompletedTask;
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
            new RagSearchRequest(new RagScope(session.TenantId, session.ClientId, session.CampaignId, runtime.KnowledgeBaseId),
                message, runtime.TopK, runtime.MinScore, runtime.AllowedDocumentTypes ?? []), ct);
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
        [JsonPropertyName("openingScript")]   public string? OpeningScript   { get; set; }
        [JsonPropertyName("startQuestionId")] public string? StartQuestionId { get; set; }
        [JsonPropertyName("closingScript")]   public string? ClosingScript   { get; set; }
        [JsonPropertyName("questions")]       public List<QuestionDefinition> Questions { get; set; } = [];
        [JsonPropertyName("intents")]         public List<IntentDefinition> Intents     { get; set; } = [];
        public bool IsMultiIntent => Intents.Count > 0;
    }

    private sealed class IntentDefinition
    {
        [JsonPropertyName("id")]                 public string Id                  { get; set; } = "";
        [JsonPropertyName("name")]               public string Name                { get; set; } = "";
        [JsonPropertyName("type")]               public string Type                { get; set; } = "collect";
        [JsonPropertyName("triggers")]           public List<string> Triggers      { get; set; } = [];
        [JsonPropertyName("questionnaire")]      public QuestionnaireDefinition? Questionnaire { get; set; }
        [JsonPropertyName("transferNumber")]     public string? TransferNumber     { get; set; }
        [JsonPropertyName("transferMessage")]    public string? TransferMessage    { get; set; }
        [JsonPropertyName("continueToIntentId")] public string? ContinueToIntentId { get; set; }
    }

    private sealed class QuestionDefinition
    {
        [JsonPropertyName("id")]             public string Id { get; set; } = string.Empty;
        [JsonPropertyName("slotId")]         public string? SlotId { get; set; }   // if set, store answer under this key instead of Id
        /// <summary>Declares the expected answer type: "text"|"number"|"date"|"datetime"|"phone"|"yesno"|"enum"</summary>
        [JsonPropertyName("slotType")]       public string? SlotType { get; set; }
        [JsonPropertyName("order")]          public int Order { get; set; }
        [JsonPropertyName("question")]       public string Question { get; set; } = string.Empty;
        [JsonPropertyName("required")]       public bool Required { get; set; } = true;
        [JsonPropertyName("validValues")]    public List<string>? ValidValues { get; set; }
        [JsonPropertyName("nextQuestionId")] public string? NextQuestionId { get; set; }
        [JsonPropertyName("branches")]       public List<QuestionBranch> Branches { get; set; } = [];
    }

    private sealed class QuestionBranch
    {
        [JsonPropertyName("when")]           public string When { get; set; } = "*";
        [JsonPropertyName("nextQuestionId")] public string? NextQuestionId { get; set; }
        [JsonPropertyName("action")]         public string? Action { get; set; }
        [JsonPropertyName("setSlots")]       public Dictionary<string, string>? SetSlots { get; set; }
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

    private sealed class CabFareSettings
    {
        public decimal BaseFare              { get; set; } = 3.50m;
        public decimal PricePerKm            { get; set; } = 1.80m;
        public decimal MinimumFare           { get; set; } = 6.00m;
        public decimal NightChargeMultiplier { get; set; } = 1.25m;
        public decimal AirportPickupFee      { get; set; } = 5.00m;
    }

    private static CabFareSettings ParseCabFareSettings(string? validationRulesJson)
    {
        if (string.IsNullOrWhiteSpace(validationRulesJson)) return new CabFareSettings();
        try
        {
            using var doc = JsonDocument.Parse(validationRulesJson);
            if (!doc.RootElement.TryGetProperty("fareSettings", out var fareEl)) return new CabFareSettings();
            return JsonSerializer.Deserialize<CabFareSettings>(fareEl.GetRawText(), JsonOpts) ?? new CabFareSettings();
        }
        catch { return new CabFareSettings(); }
    }

    private sealed class RestaurantSettings
    {
        public decimal DeliveryFee            { get; set; } = 3.99m;
        public decimal TaxRatePercent         { get; set; } = 0m;
        public string  Currency               { get; set; } = "GBP";
        public decimal FreeDeliveryThreshold  { get; set; } = 0m;
    }

    private static RestaurantSettings ParseRestaurantSettings(string? validationRulesJson)
    {
        if (string.IsNullOrWhiteSpace(validationRulesJson)) return new RestaurantSettings();
        try { return JsonSerializer.Deserialize<RestaurantSettings>(validationRulesJson, JsonOpts) ?? new RestaurantSettings(); }
        catch { return new RestaurantSettings(); }
    }
}
