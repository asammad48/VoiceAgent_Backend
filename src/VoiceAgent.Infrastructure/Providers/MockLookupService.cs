using VoiceAgent.Application.Interfaces;

namespace VoiceAgent.Infrastructure.Providers;

public class MockLookupService : ILookupService
{
    public Task<LookupResult> ExecuteAsync(string intentId, Dictionary<string, string> slots, LookupContext context, CancellationToken ct = default)
    {
        var result = intentId switch
        {
            "track_parcel" => new LookupResult(
                $"Your parcel {Slot(slots, "trackingNumber")} is with our delivery driver and is estimated to arrive between 2 pm and 4 pm today.",
                false, null),

            "order_status" => new LookupResult(
                $"Your order {Slot(slots, "orderRef")} has been picked up by our driver and is approximately 15 minutes away.",
                false, null),

            "cancel_ride" => new LookupResult(
                $"Booking {Slot(slots, "bookingRef")} has been cancelled. A refund will be processed within 3 to 5 business days to your original payment method.",
                false, null),

            "driver_status" => new LookupResult(
                "Your driver is approximately 8 minutes away and is currently heading toward your pickup point.",
                false, null),

            "reschedule_delivery" => new LookupResult(
                $"Done! Your parcel {Slot(slots, "trackingNumber")} has been rescheduled for {Slot(slots, "newDate")}. You will receive a confirmation text shortly.",
                false, null),

            "reschedule_appointment" => new LookupResult(
                $"Your appointment for {Slot(slots, "patientName")} has been rescheduled to {Slot(slots, "newDateTime")}. A confirmation text will be sent shortly.",
                false, null),

            "cancel_appointment" => new LookupResult(
                $"The appointment for {Slot(slots, "patientName")} has been successfully cancelled. If you would like to rebook in the future, please give us a call.",
                false, null),

            "doctor_availability" => new LookupResult(
                $"We have availability with Dr. Ahmed in general practice on {Slot(slots, "preferredDate")} at 10:00 am, 2:00 pm, and 4:30 pm. Would you like to book one of these slots?",
                true, "book_appointment"),

            "cod_payment" => new LookupResult(
                $"Your parcel {Slot(slots, "trackingNumber")} has a cash on delivery payment of £24.50. Please have this ready when the driver arrives.",
                false, null),

            "modify_cancel_order" => new LookupResult(
                $"I've passed your request to the kitchen for order {Slot(slots, "orderRef")}. Please note that once an order is being prepared we may not be able to make changes. You'll receive a text confirmation shortly.",
                false, null),

            "modify_order" => new LookupResult(
                $"Done! I've submitted the change request for order {Slot(slots, "trackingNumber")} — {Slot(slots, "changeRequest")}. You'll receive a confirmation text shortly.",
                false, null),

            "cancel_order" => new LookupResult(
                $"Done! Order {Slot(slots, "trackingNumber")} has been successfully cancelled. If a refund is due, it will be processed within 3 to 5 business days.",
                false, null),

            "modify_ride" => new LookupResult(
                $"Done! Booking {Slot(slots, "bookingRef")} has been updated — {Slot(slots, "changeRequest")}. Your driver has been notified and you'll receive a confirmation shortly.",
                false, null),

            "fee_location_inquiry" => new LookupResult(
                "Our standard consultation fee is £60. We are located at 12 Health Street, London. Opening hours are Monday to Friday 8 am to 6 pm, Saturday 9 am to 1 pm. Paid parking is available on site.",
                false, null),

            "menu_inquiry" => new LookupResult(
                "We have a great range of options including pizzas, burgers, salads, wraps, and our daily specials. Would you like to place an order?",
                true, "new_order"),

            "fare_estimate" => new LookupResult(
                BuildFareEstimate(slots),
                true, "book_cab"),

            _ => new LookupResult("I've looked that up for you. Is there anything else I can help with?", false, null)
        };

        return Task.FromResult(result);
    }

    private static string Slot(Dictionary<string, string> slots, string key)
        => slots.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : "[unknown]";

    private static string BuildFareEstimate(Dictionary<string, string> slots)
    {
        var pickup  = Slot(slots, "pickupLocation");
        var dropoff = Slot(slots, "dropoffLocation");

        // Use pre-calculated distance if available (set by ResolveDistanceKmAsync)
        decimal.TryParse(slots.GetValueOrDefault("distanceKm", "0"), out var distKm);
        decimal.TryParse(slots.GetValueOrDefault("estimatedFare", "0"), out var fare);

        if (distKm > 0 && fare > 0)
            return $"A fare from {pickup} to {dropoff} would be approximately £{fare:F2}. " +
                   $"This is based on roughly {distKm:F1} km. Would you like to go ahead and book?";

        return $"A fare from {pickup} to {dropoff} would be approximately £12.00 based on roughly 5 km. " +
               "Would you like to go ahead and book?";
    }
}
