using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Application.Services.Rag;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Providers;

public class ProductionLookupService(
    IAppDbContext db,
    IRagRetrievalService ragService,
    ILogger<ProductionLookupService> logger) : ILookupService
{
    public Task<LookupResult> ExecuteAsync(string intentId, Dictionary<string, string> slots, LookupContext context, CancellationToken ct = default)
        => intentId switch
        {
            // ── Menu / FAQ ────────────────────────────────────────────────────
            "menu_inquiry"           => HandleMenuInquiryAsync(context, ct),
            "fee_location_inquiry"   => HandleFeeLocationAsync(context, ct),

            // ── Courier ───────────────────────────────────────────────────────
            "track_parcel"           => HandleTrackParcelAsync(slots, context, ct),
            "reschedule_delivery"    => HandleRescheduleDeliveryAsync(slots, context, ct),
            "cod_payment"            => HandleCodPaymentAsync(slots, context, ct),
            "modify_order"           => HandleModifyOrderAsync(slots, context, ct),
            "cancel_order"           => HandleCancelOrderAsync(slots, context, ct),

            // ── Cab ───────────────────────────────────────────────────────────
            "fare_estimate"          => Task.FromResult(FareEstimate(slots)),
            "cancel_ride"            => HandleCancelRideAsync(slots, context, ct),
            "driver_status"          => HandleDriverStatusAsync(slots, context, ct),
            "modify_ride"            => HandleModifyRideAsync(slots, context, ct),

            // ── Restaurant ────────────────────────────────────────────────────
            "order_status"           => HandleOrderStatusAsync(slots, context, ct),
            "modify_cancel_order"    => HandleModifyCancelRestaurantOrderAsync(slots, context, ct),

            // ── Doctor ────────────────────────────────────────────────────────
            "doctor_availability"    => HandleDoctorAvailabilityAsync(slots, context, ct),
            "reschedule_appointment" => HandleRescheduleAppointmentAsync(slots, context, ct),
            "cancel_appointment"     => HandleCancelAppointmentAsync(slots, context, ct),

            _                        => Task.FromResult(new LookupResult("I've looked that up for you. Is there anything else I can help with?", false, null))
        };

    // ── menu_inquiry ─────────────────────────────────────────────────────────

    private async Task<LookupResult> HandleMenuInquiryAsync(LookupContext ctx, CancellationToken ct)
    {
        var categories = await db.MenuCategories
            .Where(x => x.TenantId == ctx.TenantId && x.ClientId == ctx.ClientId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.Name)
            .Take(6)
            .ToListAsync(ct);

        var items = await db.MenuItems
            .Where(x => x.TenantId == ctx.TenantId && x.ClientId == ctx.ClientId && x.IsActive && x.IsAvailable)
            .OrderBy(x => x.BasePrice)
            .Select(x => x.Name)
            .Take(5)
            .ToListAsync(ct);

        if (categories.Count > 0 || items.Count > 0)
        {
            var parts = new List<string>();
            if (categories.Count > 0) parts.Add($"categories include {string.Join(", ", categories)}");
            if (items.Count > 0)      parts.Add($"popular items are {string.Join(", ", items)}");
            return new LookupResult(
                $"On our menu, {string.Join(" and ", parts)}. Would you like to place an order?",
                true, "new_order");
        }

        return new LookupResult(
            "We have a great range of options including pizzas, burgers, salads, wraps, and our daily specials. Would you like to place an order?",
            true, "new_order");
    }

    // ── fee_location_inquiry ─────────────────────────────────────────────────

    private async Task<LookupResult> HandleFeeLocationAsync(LookupContext ctx, CancellationToken ct)
    {
        var kb = await db.KnowledgeBases
            .Where(x => x.CampaignId == ctx.CampaignId && x.TenantId == ctx.TenantId && x.IsActive)
            .FirstOrDefaultAsync(ct);

        if (kb is not null)
        {
            var scope = new RagScope(ctx.TenantId, ctx.ClientId, ctx.CampaignId, kb.Id);
            var ragResult = await ragService.SearchAsync(
                new RagSearchRequest(scope, "fee consultation charge location address opening hours", 3, 0.1m, ["FAQ", "Policy", "Script"]), ct);

            if (ragResult.Found)
            {
                var text = string.Join(" ", ragResult.Chunks.Select(c => c.ChunkText));
                logger.LogInformation("[Lookup] fee_location_inquiry answered via RAG ({Count} chunks)", ragResult.Chunks.Count);
                return new LookupResult(text, false, null);
            }
        }

        return new LookupResult(
            "Our standard consultation fee is £60. We are located at 12 Health Street, London. Opening hours are Monday to Friday 8 am to 6 pm, Saturday 9 am to 1 pm. Paid parking is available on site.",
            false, null);
    }

    // ── track_parcel ─────────────────────────────────────────────────────────

    private async Task<LookupResult> HandleTrackParcelAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef = Slot(slots, "trackingNumber");
        if (rawRef == "[unknown]")
            return new LookupResult("I didn't catch your parcel reference number. Could you please say it again?", false, null);
        var trackingRef = NormaliseRef(rawRef);
        var order = await FindCourierOrderAsync(trackingRef, ctx, ct);
        if (order is null)
            return new LookupResult(
                $"I couldn't find a parcel with reference {trackingRef}. Please double-check the reference and try again, or I can connect you to a team member.",
                false, null);

        var msg = order.Status switch
        {
            "pending"                => "Your parcel has been booked and is awaiting collection.",
            "collected"              => "Your parcel has been collected and is in transit.",
            "in_transit"             => "Your parcel is currently in transit and on its way to the destination.",
            "out_for_delivery"       => "Your parcel is with our delivery driver and is estimated to arrive between 2 pm and 4 pm today.",
            "delivered"              => "Your parcel has been successfully delivered.",
            "cancelled"              => "This order has been cancelled.",
            "reschedule_requested"   => "A reschedule has been requested and our team is processing it.",
            "modification_requested" => "A modification request is being processed by our team.",
            _                        => $"Your parcel status is: {order.Status}."
        };

        logger.LogInformation("[Lookup] track_parcel orderId={OrderId} status={Status}", order.Id, order.Status);
        return new LookupResult(msg, false, null);
    }

    // ── reschedule_delivery ───────────────────────────────────────────────────

    private async Task<LookupResult> HandleRescheduleDeliveryAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef  = Slot(slots, "trackingNumber");
        if (rawRef == "[unknown]")
            return new LookupResult("I didn't catch your parcel reference number. Could you please say it again?", false, null);
        var trackingRef = NormaliseRef(rawRef);
        var newDate     = Slot(slots, "newDate");
        var order       = await FindCourierOrderAsync(trackingRef, ctx, ct);

        if (order is null)
            return new LookupResult(
                $"I couldn't find a parcel with reference {trackingRef}. Please double-check the reference and try again.",
                false, null);

        if (order.Status == "cancelled")
            return new LookupResult(
                $"Parcel {trackingRef} has been cancelled and cannot be rescheduled. Is there anything else I can help with?",
                false, null);

        if (order.Status == "delivered")
            return new LookupResult(
                $"Parcel {trackingRef} has already been delivered and cannot be rescheduled.",
                false, null);

        if (order.Status == "out_for_delivery")
            return new LookupResult(
                $"Parcel {trackingRef} is already out for delivery today and cannot be rescheduled. We'll attempt delivery shortly.",
                false, null);

        if (order.Status == "in_transit")
        {
            order.Status = "reschedule_requested";
            AppendToFinalResult(order, "rescheduleDate", newDate);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("[Lookup] reschedule_delivery (in_transit) orderId={OrderId} newDate={NewDate}", order.Id, newDate);
            return new LookupResult(
                $"Your parcel {trackingRef} is currently in transit. A reschedule request for {newDate} has been submitted, but please note it may take up to 48 hours to process and cannot be guaranteed.",
                false, null);
        }

        if (order.Status == "collected")
        {
            order.Status = "reschedule_requested";
            AppendToFinalResult(order, "rescheduleDate", newDate);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("[Lookup] reschedule_delivery (collected) orderId={OrderId} newDate={NewDate}", order.Id, newDate);
            return new LookupResult(
                $"Your parcel {trackingRef} has already been collected by our courier. A reschedule request for {newDate} has been submitted, but please note it may take up to 24 hours to process.",
                false, null);
        }

        order.Status = "reschedule_requested";
        AppendToFinalResult(order, "rescheduleDate", newDate);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Lookup] reschedule_delivery orderId={OrderId} newDate={NewDate}", order.Id, newDate);
        return new LookupResult(
            $"Done! Your parcel {trackingRef} has been rescheduled for {newDate}. You will receive a confirmation text shortly.",
            false, null);
    }

    // ── cod_payment ───────────────────────────────────────────────────────────

    private async Task<LookupResult> HandleCodPaymentAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef = Slot(slots, "trackingNumber");
        if (rawRef == "[unknown]")
            return new LookupResult("I didn't catch your parcel reference number. Could you please say it again?", false, null);
        var trackingRef = NormaliseRef(rawRef);
        var order       = await FindCourierOrderAsync(trackingRef, ctx, ct);
        if (order is null)
            return new LookupResult(
                $"I couldn't find a parcel with reference {trackingRef}. Please double-check the reference and try again.",
                false, null);

        if (order.Status == "cancelled")
            return new LookupResult(
                $"Parcel {trackingRef} has been cancelled. There is no cash on delivery payment due.",
                false, null);

        if (order.Status == "delivered")
            return new LookupResult(
                $"Parcel {trackingRef} has already been delivered. The cash on delivery payment should have been collected at the time of delivery.",
                false, null);

        // Resolve COD amount via linked quote
        string amountText = "an amount to be confirmed";
        if (order.CourierQuoteId.HasValue)
        {
            var quote = await db.CourierQuotes.FindAsync([order.CourierQuoteId.Value], cancellationToken: ct);
            if (quote is not null)
                amountText = $"{quote.Currency}{quote.Total:F2}";
        }

        logger.LogInformation("[Lookup] cod_payment orderId={OrderId} amount={Amount}", order.Id, amountText);
        return new LookupResult(
            $"Your parcel {trackingRef} has a cash on delivery payment of {amountText}. Please have this ready when the driver arrives.",
            false, null);
    }

    // ── modify_order ─────────────────────────────────────────────────────────

    private async Task<LookupResult> HandleModifyOrderAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef        = Slot(slots, "trackingNumber");
        if (rawRef == "[unknown]")
            return new LookupResult("I didn't catch your parcel reference number. Could you please say it again?", false, null);
        var trackingRef   = NormaliseRef(rawRef);
        var changeRequest = Slot(slots, "changeRequest");
        var order         = await FindCourierOrderAsync(trackingRef, ctx, ct);

        if (order is null)
            return new LookupResult(
                $"I couldn't find an order with reference {trackingRef}. Please double-check the reference and try again.",
                false, null);

        if (order.Status is "cancelled" or "delivered")
            return new LookupResult(
                $"Order {trackingRef} is {order.Status} and can no longer be modified. Is there anything else I can help with?",
                false, null);

        if (order.Status is "collected" or "in_transit" or "out_for_delivery")
            return new LookupResult(
                $"Order {trackingRef} is {order.Status.Replace("_", " ")} and its details can no longer be modified. Is there anything else I can help with?",
                false, null);

        var replacingModification = order.Status == "modification_requested";
        order.Status = "modification_requested";
        AppendToFinalResult(order, "modificationRequest", changeRequest);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Lookup] modify_order orderId={OrderId} request={Request}", order.Id, changeRequest);
        var modMsg = replacingModification
            ? $"Done! I've updated your modification request for order {trackingRef} — {changeRequest}. Your previous request has been replaced."
            : $"Done! I've submitted the change request for order {trackingRef} — {changeRequest}. You'll receive a confirmation text shortly.";
        return new LookupResult(modMsg, false, null);
    }

    // ── cancel_order ─────────────────────────────────────────────────────────

    private async Task<LookupResult> HandleCancelOrderAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef = Slot(slots, "trackingNumber");
        if (rawRef == "[unknown]")
            return new LookupResult("I didn't catch your parcel reference number. Could you please say it again?", false, null);
        var trackingRef = NormaliseRef(rawRef);
        var order       = await FindCourierOrderAsync(trackingRef, ctx, ct);

        if (order is null)
            return new LookupResult(
                $"I couldn't find an order with reference {trackingRef}. Please double-check the reference and try again.",
                false, null);

        if (order.Status == "cancelled")
            return new LookupResult(
                $"Order {trackingRef} has already been cancelled. Is there anything else I can help with?",
                false, null);

        if (order.Status == "delivered")
            return new LookupResult(
                $"Order {trackingRef} has already been delivered and cannot be cancelled. Is there anything else I can help with?",
                false, null);

        if (order.Status is "collected" or "in_transit")
            return new LookupResult(
                $"Order {trackingRef} has already been collected by our courier and can no longer be cancelled. Is there anything else I can help with?",
                false, null);

        if (order.Status == "out_for_delivery")
            return new LookupResult(
                $"Order {trackingRef} is out for delivery today and can no longer be cancelled. If you have an issue after delivery, please contact us.",
                false, null);

        var hadPendingRequest = order.Status is "modification_requested" or "reschedule_requested";
        order.Status = "cancelled";
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Lookup] cancel_order orderId={OrderId}", order.Id);
        var cancelMsg = $"Done! Order {trackingRef} has been successfully cancelled. If a refund is due, it will be processed within 3 to 5 business days.";
        if (hadPendingRequest)
            cancelMsg += " Please note that any pending modification or reschedule request has been discarded.";
        return new LookupResult(cancelMsg, false, null);
    }

    // ── fare_estimate ─────────────────────────────────────────────────────────

    private static LookupResult FareEstimate(Dictionary<string, string> slots)
    {
        var pickup  = Slot(slots, "pickupLocation");
        var dropoff = Slot(slots, "dropoffLocation");
        decimal.TryParse(slots.GetValueOrDefault("distanceKm",    "0"), out var distKm);
        decimal.TryParse(slots.GetValueOrDefault("estimatedFare", "0"), out var fare);

        if (distKm > 0 && fare > 0)
            return new LookupResult(
                $"A fare from {pickup} to {dropoff} would be approximately £{fare:F2}, based on roughly {distKm:F1} km. Would you like to go ahead and book?",
                true, "book_cab");

        return new LookupResult(
            $"A fare from {pickup} to {dropoff} would be approximately £12.00 based on roughly 5 km. Would you like to go ahead and book?",
            true, "book_cab");
    }

    // ── cancel_ride ───────────────────────────────────────────────────────────

    private async Task<LookupResult> HandleCancelRideAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef  = Slot(slots, "bookingRef");
        if (rawRef == "[unknown]")
            return new LookupResult("I didn't catch your booking reference number. Could you please say it again?", false, null);
        var bookingRef = NormaliseRef(rawRef);
        var booking    = await FindCabBookingAsync(bookingRef, ctx, ct);

        if (booking is null)
            return new LookupResult(
                $"I couldn't find a booking with reference {bookingRef}. Please double-check the reference and try again.",
                false, null);

        if (booking.Status == "cancelled")
            return new LookupResult(
                $"Booking {bookingRef} has already been cancelled. Is there anything else I can help with?",
                false, null);

        if (booking.Status == "completed")
            return new LookupResult(
                $"Booking {bookingRef} has already been completed and cannot be cancelled. Is there anything else I can help with?",
                false, null);

        if (booking.Status == "in_progress")
            return new LookupResult(
                $"Your ride is currently in progress and cannot be cancelled. Please speak with the driver directly.",
                false, null);

        if (booking.Status == "arrived")
            return new LookupResult(
                $"Your driver has already arrived at the pickup point. Cancellation is no longer possible at this stage.",
                false, null);

        if (booking.Status == "en_route")
            return new LookupResult(
                $"Your driver is already on the way to your pickup point and the booking cannot be cancelled at this stage. Please speak with the driver directly.",
                false, null);

        if (TryParseScheduledDateTime(booking.PickupDateTime, out var pickupDt) && pickupDt < DateTime.Now)
            return new LookupResult(
                $"The scheduled pickup time for booking {bookingRef} has already passed. This booking is no longer active. Is there anything else I can help with?",
                false, null);

        var hadPendingModification = booking.Status == "modification_requested";
        booking.Status = "cancelled";
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Lookup] cancel_ride bookingId={BookingId}", booking.Id);
        var cancelMsg = $"Booking {bookingRef} has been cancelled. A refund will be processed within 3 to 5 business days.";
        if (hadPendingModification)
            cancelMsg += " Please note that your pending modification request has been discarded.";
        return new LookupResult(cancelMsg, false, null);
    }

    // ── driver_status ─────────────────────────────────────────────────────────

    private async Task<LookupResult> HandleDriverStatusAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef  = Slot(slots, "bookingRef");
        if (rawRef == "[unknown]")
            return new LookupResult("I didn't catch your booking reference number. Could you please say it again?", false, null);
        var bookingRef = NormaliseRef(rawRef);
        var booking    = await FindCabBookingAsync(bookingRef, ctx, ct);

        if (booking is null)
            return new LookupResult(
                $"I couldn't find a booking with reference {bookingRef}. Please double-check the reference and try again.",
                false, null);

        var msg = booking.Status switch
        {
            "pending"                => "Your booking is confirmed and a driver will be assigned shortly.",
            "driver_assigned"        => "A driver has been assigned to your booking and is on their way.",
            "en_route"               => "Your driver is approximately 8 minutes away and is currently heading toward your pickup point.",
            "arrived"                => "Your driver has arrived at the pickup location.",
            "in_progress"            => "Your journey is currently in progress.",
            "completed"              => "Your ride has been completed. Thank you for travelling with us!",
            "cancelled"              => "This booking has been cancelled.",
            "modification_requested" => "A modification request is being processed for this booking.",
            _                        => "Your driver is approximately 8 minutes away and is currently heading toward your pickup point."
        };

        logger.LogInformation("[Lookup] driver_status bookingId={BookingId} status={Status}", booking.Id, booking.Status);
        return new LookupResult(msg, false, null);
    }

    // ── modify_ride ───────────────────────────────────────────────────────────

    private async Task<LookupResult> HandleModifyRideAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef        = Slot(slots, "bookingRef");
        if (rawRef == "[unknown]")
            return new LookupResult("I didn't catch your booking reference number. Could you please say it again?", false, null);
        var bookingRef    = NormaliseRef(rawRef);
        var changeRequest = Slot(slots, "changeRequest");
        var booking       = await FindCabBookingAsync(bookingRef, ctx, ct);

        if (booking is null)
            return new LookupResult(
                $"I couldn't find a booking with reference {bookingRef}. Please double-check the reference and try again.",
                false, null);

        if (booking.Status is "cancelled" or "completed")
            return new LookupResult(
                $"Booking {bookingRef} is {booking.Status} and can no longer be modified. Is there anything else I can help with?",
                false, null);

        if (booking.Status is "en_route" or "arrived" or "in_progress")
            return new LookupResult(
                $"Booking {bookingRef} can no longer be modified — your driver is {booking.Status.Replace("_", " ")}. Is there anything else I can help with?",
                false, null);

        if (TryParseScheduledDateTime(booking.PickupDateTime, out var pickupDt) && pickupDt < DateTime.Now)
            return new LookupResult(
                $"The scheduled pickup time for booking {bookingRef} has already passed. This booking is no longer active. Is there anything else I can help with?",
                false, null);

        // Apply structured changes where possible
        var replacingModification = booking.Status == "modification_requested";
        ApplyCabBookingChange(booking, changeRequest);
        booking.Status = "modification_requested";
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Lookup] modify_ride bookingId={BookingId} request={Request}", booking.Id, changeRequest);
        var modMsg = replacingModification
            ? $"Done! I've updated your modification request for booking {bookingRef} — {changeRequest}. Your previous request has been replaced and you'll receive a confirmation shortly."
            : $"Done! Booking {bookingRef} has been updated — {changeRequest}. Your driver has been notified and you'll receive a confirmation shortly.";
        return new LookupResult(modMsg, false, null);
    }

    // ── order_status (Restaurant) ─────────────────────────────────────────────

    private async Task<LookupResult> HandleOrderStatusAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef = Slot(slots, "orderRef");
        var phone  = Slot(slots, "phone");

        if (rawRef == "[unknown]" && phone == "[unknown]")
            return new LookupResult("I didn't catch your order reference or phone number. Could you please provide your order reference, or the phone number used to place the order?", false, null);

        var orderRef = NormaliseRef(rawRef);
        var order = await FindRestaurantOrderAsync(orderRef, ctx, ct);
        if (order is null && phone != "[unknown]")
        {
            order = await db.RestaurantOrders
                .Where(x => x.TenantId == ctx.TenantId && x.ClientId == ctx.ClientId && x.Phone == phone)
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefaultAsync(ct);
        }

        if (order is null)
            return new LookupResult(
                "I couldn't find that order. Please check the reference or the phone number used and try again.",
                false, null);

        var msg = order.Status switch
        {
            "pending"                => "Your order has been received and is being prepared.",
            "confirmed"              => "Your order has been confirmed and is being prepared in the kitchen.",
            "preparing"              => "Your order is currently being prepared in the kitchen.",
            "ready"                  => "Your order is ready and waiting for the driver.",
            "out_for_delivery"       => "Your order has been picked up by our driver and is approximately 15 minutes away.",
            "delivered"              => "Your order has been delivered. Enjoy your meal!",
            "cancelled"              => "This order has been cancelled.",
            "modification_requested" => "Your modification request is being processed by our team.",
            _                        => $"Your order status is: {order.Status}."
        };

        logger.LogInformation("[Lookup] order_status orderId={OrderId} status={Status}", order.Id, order.Status);
        return new LookupResult(msg, false, null);
    }

    // ── modify_cancel_order (Restaurant) ─────────────────────────────────────

    private async Task<LookupResult> HandleModifyCancelRestaurantOrderAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef        = Slot(slots, "orderRef");
        if (rawRef == "[unknown]")
            return new LookupResult("I didn't catch your order reference number. Could you please say it again?", false, null);
        var orderRef      = NormaliseRef(rawRef);
        var changeRequest = Slot(slots, "changeRequest");
        var order         = await FindRestaurantOrderAsync(orderRef, ctx, ct);

        if (order is null)
            return new LookupResult(
                $"I couldn't find an order with reference {orderRef}. Please check the reference and try again.",
                false, null);

        if (order.Status == "cancelled")
            return new LookupResult(
                $"Order {orderRef} has already been cancelled. Is there anything else I can help with?",
                false, null);

        if (order.Status is "delivered" or "out_for_delivery")
            return new LookupResult(
                $"Order {orderRef} is already {order.Status.Replace("_", " ")} and can no longer be changed. Is there anything else I can help with?",
                false, null);

        if (order.Status is "preparing" or "ready")
            return new LookupResult(
                $"Order {orderRef} is already {order.Status} in the kitchen and can no longer be changed or cancelled. Is there anything else I can help with?",
                false, null);

        var isCancellation = Regex.IsMatch(changeRequest, @"\b(cancel|cancellation)\b", RegexOptions.IgnoreCase);
        if (isCancellation)
        {
            var hadPendingModification = order.Status == "modification_requested";
            order.Status = "cancelled";
            await db.SaveChangesAsync(ct);
            logger.LogInformation("[Lookup] cancel_restaurant_order orderId={OrderId}", order.Id);
            var cancelMsg = $"Order {orderRef} has been successfully cancelled. You'll receive a confirmation text shortly.";
            if (hadPendingModification)
                cancelMsg += " Please note that your pending modification request has been discarded.";
            return new LookupResult(cancelMsg, false, null);
        }

        var replacingModification = order.Status == "modification_requested";
        order.Status = "modification_requested";
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Lookup] modify_restaurant_order orderId={OrderId} request={Request}", order.Id, changeRequest);

        string modMsg;
        if (replacingModification)
            modMsg = $"I've updated your modification request for order {orderRef} — {changeRequest}. Your previous request has been replaced.";
        else if ((DateTime.UtcNow - order.CreatedOn).TotalMinutes > 5)
            modMsg = $"Your order was placed more than 5 minutes ago and the kitchen may have already started. I've passed your request for order {orderRef} — {changeRequest} — but please note it may not be possible to make changes at this stage.";
        else
            modMsg = $"I've passed your request to the kitchen for order {orderRef} — {changeRequest}. You'll receive a confirmation text shortly.";
        return new LookupResult(modMsg, false, null);
    }

    // ── doctor_availability ───────────────────────────────────────────────────

    private async Task<LookupResult> HandleDoctorAvailabilityAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var preferredDate = Slot(slots, "preferredDate");
        var specialty     = Slot(slots, "specialty");

        if (TryParseScheduledDateTime(preferredDate, out var parsedDate) && parsedDate.Date < DateTime.Today)
            return new LookupResult(
                "That date has already passed. Could you please give me a future date you'd like to check?",
                false, null);

        // Standard clinic time slots
        string[] allSlots = ["9:00 am", "10:00 am", "11:00 am", "2:00 pm", "3:00 pm", "4:30 pm"];

        // Find already-booked times on the preferred date for this campaign
        var existingTimes = await db.DoctorAppointments
            .Where(x => x.TenantId == ctx.TenantId && x.ClientId == ctx.ClientId
                     && x.CampaignId == ctx.CampaignId && x.Status != "cancelled"
                     && x.PreferredDateTime.Contains(preferredDate))
            .Select(x => x.PreferredDateTime)
            .ToListAsync(ct);

        var available = allSlots
            .Where(s => !existingTimes.Any(t => t.Contains(s, StringComparison.OrdinalIgnoreCase)))
            .Take(3)
            .ToList();

        if (available.Count == 0)
        {
            logger.LogInformation("[Lookup] doctor_availability date={Date} fully booked", preferredDate);
            return new LookupResult(
                $"Unfortunately we are fully booked on {preferredDate}. Would you like to try a different date?",
                false, null);
        }

        var slotList = string.Join(", ", available);
        logger.LogInformation("[Lookup] doctor_availability date={Date} slots={Slots}", preferredDate, slotList);
        return new LookupResult(
            $"We have availability on {preferredDate} at {slotList}. Would you like to book one of these slots?",
            true, "book_appointment");
    }

    // ── reschedule_appointment ────────────────────────────────────────────────

    private async Task<LookupResult> HandleRescheduleAppointmentAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef      = Slot(slots, "appointmentRef");
        var patientName = Slot(slots, "patientName");
        if (rawRef == "[unknown]" && patientName == "[unknown]")
            return new LookupResult("I didn't catch your appointment reference or name. Could you please provide your appointment reference number, or the name the appointment is booked under?", false, null);
        var appointmentRef = NormaliseRef(rawRef);
        var newDateTime    = Slot(slots, "newDateTime");
        var appt           = await FindDoctorAppointmentAsync(appointmentRef, patientName, ctx, ct);

        if (appt is null)
            return new LookupResult(
                $"I couldn't find an appointment with that reference. Please double-check and try again.",
                false, null);

        if (appt.Status == "cancelled")
            return new LookupResult(
                $"The appointment for {appt.PatientName} has already been cancelled. Please call us to book a new one.",
                false, null);

        if (appt.Status == "completed")
            return new LookupResult(
                "That appointment has already taken place and cannot be rescheduled. Please call us to book a new one.",
                false, null);

        if (TryParseScheduledDateTime(appt.PreferredDateTime, out var apptDt))
        {
            if (apptDt.Date < DateTime.Today)
                return new LookupResult(
                    "That appointment date has already passed. Please call us to book a new one.",
                    false, null);

            if (apptDt.Date == DateTime.Today)
                return new LookupResult(
                    "I'm unable to reschedule same-day appointments by phone. Please call the clinic directly.",
                    false, null);

            if ((apptDt - DateTime.Now).TotalHours < 24)
                return new LookupResult(
                    "This appointment is within 24 hours and cannot be rescheduled by phone. Please call the clinic directly.",
                    false, null);
        }

        appt.PreferredDateTime = newDateTime;
        appt.Status = "rescheduled";
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Lookup] reschedule_appointment apptId={ApptId} newDateTime={DateTime}", appt.Id, newDateTime);
        return new LookupResult(
            $"Your appointment for {appt.PatientName} has been rescheduled to {newDateTime}. A confirmation will be sent to {appt.Phone} shortly.",
            false, null);
    }

    // ── cancel_appointment ────────────────────────────────────────────────────

    private async Task<LookupResult> HandleCancelAppointmentAsync(Dictionary<string, string> slots, LookupContext ctx, CancellationToken ct)
    {
        var rawRef      = Slot(slots, "appointmentRef");
        var patientName = Slot(slots, "patientName");
        if (rawRef == "[unknown]" && patientName == "[unknown]")
            return new LookupResult("I didn't catch your appointment reference or name. Could you please provide your appointment reference number, or the name the appointment is booked under?", false, null);
        var appointmentRef = NormaliseRef(rawRef);
        var appt           = await FindDoctorAppointmentAsync(appointmentRef, patientName, ctx, ct);

        if (appt is null)
            return new LookupResult(
                "I couldn't find an appointment with that reference. Please double-check and try again.",
                false, null);

        if (appt.Status == "cancelled")
            return new LookupResult(
                $"The appointment for {appt.PatientName} has already been cancelled. Is there anything else I can help with?",
                false, null);

        if (appt.Status == "completed")
            return new LookupResult(
                "That appointment has already taken place and cannot be cancelled. Is there anything else I can help with?",
                false, null);

        if (TryParseScheduledDateTime(appt.PreferredDateTime, out var apptDt))
        {
            if (apptDt.Date < DateTime.Today)
                return new LookupResult(
                    "That appointment date has already passed. Is there anything else I can help with?",
                    false, null);

            if (apptDt.Date == DateTime.Today)
                return new LookupResult(
                    "I'm unable to cancel same-day appointments by phone. Please call the clinic directly for urgent changes.",
                    false, null);

            if ((apptDt - DateTime.Now).TotalHours < 24)
                return new LookupResult(
                    "This appointment is within 24 hours and cannot be cancelled by phone. Please call the clinic directly.",
                    false, null);
        }

        appt.Status = "cancelled";
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Lookup] cancel_appointment apptId={ApptId}", appt.Id);
        return new LookupResult(
            $"The appointment for {appt.PatientName} has been successfully cancelled. Please give us a call to rebook whenever you're ready.",
            false, null);
    }

    // ── Entity finders ────────────────────────────────────────────────────────

    private async Task<CourierOrder?> FindCourierOrderAsync(string shortRef, LookupContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(shortRef)) return null;
        var candidates = await db.CourierOrders
            .Where(x => x.TenantId == ctx.TenantId && x.ClientId == ctx.ClientId)
            .OrderByDescending(x => x.CreatedOn)
            .Take(200)
            .ToListAsync(ct);
        return candidates.FirstOrDefault(x =>
            x.Id.ToString("N")[..8].Equals(shortRef, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<CabBooking?> FindCabBookingAsync(string shortRef, LookupContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(shortRef)) return null;
        var candidates = await db.CabBookings
            .Where(x => x.TenantId == ctx.TenantId && x.ClientId == ctx.ClientId)
            .OrderByDescending(x => x.CreatedOn)
            .Take(200)
            .ToListAsync(ct);
        return candidates.FirstOrDefault(x =>
            x.Id.ToString("N")[..8].Equals(shortRef, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<RestaurantOrder?> FindRestaurantOrderAsync(string shortRef, LookupContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(shortRef)) return null;
        var candidates = await db.RestaurantOrders
            .Where(x => x.TenantId == ctx.TenantId && x.ClientId == ctx.ClientId)
            .OrderByDescending(x => x.CreatedOn)
            .Take(200)
            .ToListAsync(ct);
        return candidates.FirstOrDefault(x =>
            x.Id.ToString("N")[..8].Equals(shortRef, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<DoctorAppointment?> FindDoctorAppointmentAsync(string shortRef, string patientName, LookupContext ctx, CancellationToken ct)
    {
        // Try by short ref first
        if (!string.IsNullOrWhiteSpace(shortRef))
        {
            var candidates = await db.DoctorAppointments
                .Where(x => x.TenantId == ctx.TenantId && x.ClientId == ctx.ClientId)
                .OrderByDescending(x => x.CreatedOn)
                .Take(200)
                .ToListAsync(ct);
            var byRef = candidates.FirstOrDefault(x =>
                x.Id.ToString("N")[..8].Equals(shortRef, StringComparison.OrdinalIgnoreCase));
            if (byRef is not null) return byRef;
        }

        // Fallback: match by patient name (most recent)
        if (!string.IsNullOrWhiteSpace(patientName) && patientName != "[unknown]")
        {
            return await db.DoctorAppointments
                .Where(x => x.TenantId == ctx.TenantId && x.ClientId == ctx.ClientId
                         && EF.Functions.ILike(x.PatientName, $"%{patientName.Trim()}%"))
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefaultAsync(ct);
        }

        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Slot(Dictionary<string, string> slots, string key)
        => slots.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : "[unknown]";

    private static string NormaliseRef(string raw)
        => raw.Trim().ToUpperInvariant().Replace(" ", "");

    private static void AppendToFinalResult(CourierOrder order, string key, string value)
    {
        try
        {
            var data = string.IsNullOrWhiteSpace(order.FinalResultJson)
                ? []
                : JsonSerializer.Deserialize<Dictionary<string, object>>(order.FinalResultJson) ?? [];
            data[key] = value;
            data[$"{key}At"] = DateTime.UtcNow.ToString("O");
            order.FinalResultJson = JsonSerializer.Serialize(data);
        }
        catch { /* non-fatal; status update is what matters */ }
    }

    private static bool TryParseScheduledDateTime(string raw, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(raw) || raw == "[unknown]") return false;
        return DateTime.TryParse(raw.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    private static void ApplyCabBookingChange(CabBooking booking, string changeRequest)
    {
        // Best-effort structured update: extract new destination if the caller mentioned one
        var destMatch = Regex.Match(changeRequest,
            @"(?:destination|drop.?off|drop to|going to|heading to)\s+(.+)",
            RegexOptions.IgnoreCase);
        if (destMatch.Success)
            booking.DropoffLocation = destMatch.Groups[1].Value.Trim();

        // Extract new vehicle type if mentioned
        if (Regex.IsMatch(changeRequest, @"\bexecutive\b", RegexOptions.IgnoreCase))
            booking.VehicleType = "executive";
        else if (Regex.IsMatch(changeRequest, @"\b6.seater\b", RegexOptions.IgnoreCase))
            booking.VehicleType = "6-seater";
        else if (Regex.IsMatch(changeRequest, @"\bstandard\b", RegexOptions.IgnoreCase))
            booking.VehicleType = "standard";
    }
}
