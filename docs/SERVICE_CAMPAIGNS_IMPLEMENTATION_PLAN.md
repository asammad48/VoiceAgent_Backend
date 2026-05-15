# Service Campaigns — Implementation Plan

How we tackle the four service campaign gaps in a safe, incremental order.

---

## Guiding Principles

1. **Domain first, then infrastructure, then orchestrator** — add entities and migrations before touching conversation logic. A bad migration is harder to undo than a bad method.
2. **One campaign at a time** — each phase is independently deployable and testable via `POST /api/demo/message`.
3. **No breaking changes** — existing sessions keep working. New logic is additive or hidden behind slot presence checks.
4. **Seed data before logic** — any code that queries the DB for pricing data must have seed records ready or it will crash in dev.

---

## Phase Overview

```
Phase A  Courier     — proactive quote, band pricing, CourierOrder save
Phase B  Restaurant  — config-driven fees, deal discounts, CustomerName/Phone on order
Phase C  Cab         — CabBooking entity, real fare calc, airport/night surcharge
Phase D  Doctor      — DoctorAppointment entity, appointment save, branch-aware availability
```

Estimated effort per phase: **2–4 hours** each. Total: **1–2 days**.

---

## Phase A — Courier Service

### Goal
User hears a price estimate during the conversation, not just at the final "confirm" step. Saved records include both `CourierQuote` and `CourierOrder`.

### Step-by-step

**A1 — Create `CourierSeed.cs`**
- File: `src/VoiceAgent.Infrastructure/Persistence/Seed/CourierSeed.cs`
- One static `CourierPricingProfile` record with `BaseFee`, `PricePerKm`, `PricePerKg`, `MinimumFee`, and `SettingsJson` containing `sameDayFee` / `fragileFee` / `documentFee`.
- Assign a fixed `Guid` so the seeder is idempotent.

**A2 — Update `DatabaseSeeder.cs`**
- Add upsert for `CourierSeed.PricingProfile`.
- Pattern: `if (!await db.CourierPricingProfiles.AnyAsync(x => x.Id == CourierSeed.PricingProfile.Id)) db.CourierPricingProfiles.Add(...)`.

**A3 — Verify `IAppDbContext` + `AppDbContext`**
- Confirm `CourierDistanceBands`, `CourierWeightBands`, `CourierOrders` are declared as `DbSet<>` in both the interface and the EF context.
- If missing, add them and run a migration if the tables don't exist yet.

**A4 — Add `CalculateCourierFareAsync` to orchestrator**
- Private async method.
- Query `CourierDistanceBand` rows for the profile; use band `Fee` if a matching band exists, else fall back to `PricePerKm * distanceKm`.
- Same logic for `CourierWeightBand`.
- Deserialise `SettingsJson` for surcharges.
- Return `Math.Max(minimumFee, base + surcharges)`.

**A5 — Rewrite `HandleCourierExtrasAsync`**
- **Trigger condition:** `pickupAddress` + `dropoffAddress` + `weightKg` all present AND `estimatedFare` not yet in slots.
- Call `ResolveDistanceKmAsync` → `CalculateCourierFareAsync`.
- Store `slots["distanceKm"]` and `slots["estimatedFare"]` and `slots["estimatedCurrency"]`.
- Reply with fare preview: *"That's approximately {X} km. Estimated cost for a {weight} kg {packageType} package: {currency}{fare}. Shall I go ahead and book this?"*
- **Confirm path:** read pre-stored `distanceKm` (skip second OSRM call), save `CourierQuote` + `CourierOrder`.

**A6 — Test via demo**
- Start courier session.
- Provide addresses and weight.
- Verify the fare reply appears before "confirm".
- Say "confirm" and verify `CourierQuote` + `CourierOrder` rows exist in DB.

---

## Phase B — Restaurant Order

### Goal
Delivery fee and tax come from config. Deal selections reduce the total. `CustomerName` and `Phone` are saved on the order. User hears an itemised cart before confirming.

### Step-by-step

**B1 — Update `CampaignConfigurationSeed.cs`**
- Add `ValidationRulesJson` to the Restaurant config record:
  ```json
  {
    "deliveryFee": 3.99,
    "taxRatePercent": 0.0,
    "currency": "GBP",
    "freeDeliveryThreshold": 20.0
  }
  ```

**B2 — Add `RestaurantSettings` inner class to orchestrator**
- Deserialise from `config?.ValidationRulesJson`.
- Used by `HandleRestaurantExtrasAsync` and `BuildSummaryAndAwaitConfirmation`.

**B3 — Replace hardcoded `3.99m` in `HandleRestaurantExtrasAsync`**
- Read `settings.DeliveryFee` from config.
- Apply free-delivery threshold: if `subtotal >= settings.FreeDeliveryThreshold && threshold > 0` → fee = 0.
- Calculate `tax = subtotal * (settings.TaxRatePercent / 100)`.
- New total = `subtotal + fee + tax`.

**B4 — Fix deal application**
- When user names a deal (matched from `RestaurantDeal` records), record `slots["appliedDeal"]` and store the deal price as the cart total adjustment.
- Derive `discount = regularSubtotal - dealPrice` and store in `slots["discount"]`.

**B5 — Populate missing `RestaurantOrder` fields**
- `CustomerName`, `Phone`, `Tax`, `Discount` — all populated from slots before `db.RestaurantOrders.Add(order)`.

**B6 — Itemised cart read-back**
- In `BuildSummaryAndAwaitConfirmation` for `CampaignType.RestaurantOrder`, call a new `BuildRestaurantCartSummary(slots, settings)` helper instead of the generic numbered summary.
- Output: *"Here is your order: 2× Chicken Burger £10.00, 1× Coke £2.50. Subtotal £12.50, delivery £3.99, total £16.49."*

**B7 — Test via demo**
- Add items. Say "total". Verify correct fee + tax.
- Say "confirm". Verify `RestaurantOrder` row has `CustomerName`, `Phone`, `Tax`, `Discount` populated.

---

## Phase C — Cab Booking

### Goal
User receives a real fare estimate (with night surcharge and airport fee where applicable) mid-conversation. A `CabBooking` DB row is saved at confirmation.

### Step-by-step

**C1 — Create `CabBooking` entity**
- File: `src/VoiceAgent.Domain/Cab/CabBooking.cs`
- Fields: see `SERVICE_CAMPAIGNS_CHANGES.md` § 3.1.

**C2 — Register in `IAppDbContext` + `AppDbContext`**
- Add `DbSet<CabBooking> CabBookings`.

**C3 — Run EF migration**
```
dotnet ef migrations add AddCabBooking --project src/VoiceAgent.Infrastructure --startup-project src/VoiceAgent.Api
```

**C4 — Add `CabFareSettings` inner class + `ParseCabFareSettings` helper**
- Deserialise from nested `fareSettings` key inside `ValidationRulesJson`.
- Fallback to `CabSeed` defaults if JSON is absent.

**C5 — Add `CalculateCabFare` static helper**
- Inputs: `settings`, `distanceKm`, `pickupDateTime` (string), `isAirportPickup`.
- Night surcharge: parse hour from `pickupDateTime`; multiply if 22:00–06:00.
- Airport fee: check `IsAirportAddress(pickup)` or `IsAirportAddress(dropoff)`.
- Return `Math.Max(settings.MinimumFare, base)`.

**C6 — Add `IsAirportAddress` static helper**
- Regex match on common UK airport names and IATA codes.

**C7 — Rewrite `HandleCabExtras` → `HandleCabExtrasAsync`**
- **Trigger:** all 5 booking slots present AND `estimatedFare` not yet in slots.
- Call `ResolveDistanceKmAsync` + `CalculateCabFare`.
- Store `slots["distanceKm"]`, `slots["estimatedFare"]`, `slots["estimatedCurrency"]`.
- Reply with fare quote: *"Your {vehicleType} from {pickup} to {dropoff} is approximately {distance} km. Estimated fare: £{fare}. Shall I confirm your booking?"*
- **Confirm path:** save `CabBooking` entity to DB.
- Keep existing guards (helicopter rejection, >10 passengers, human handoff).

**C8 — Update `HandleCampaignSpecificAsync`**
- Make `CabBooking` case `await HandleCabExtrasAsync(...)`.

**C9 — Fix `BuildFinalResult` for `CabBooking`**
- Replace `estimatedFare: "£18.00"` with `slots.GetValueOrDefault("estimatedFare", "TBC")`.
- Add `distanceKm`, `currency`, `bookingId` fields.

**C10 — Test via demo**
- Provide pickup/dropoff (one being an airport).
- Provide time after 22:00. Verify night surcharge applied.
- Say "confirm". Verify `CabBooking` row in DB. Verify final result has real fare.

---

## Phase D — Doctor Appointment

### Goal
Appointment saved to a dedicated DB table. Branch slot used in availability messages.

### Step-by-step

**D1 — Create `DoctorAppointment` entity**
- File: `src/VoiceAgent.Domain/Doctor/DoctorAppointment.cs`
- Fields: see `SERVICE_CAMPAIGNS_CHANGES.md` § 4.1.

**D2 — Register in `IAppDbContext` + `AppDbContext`**
- Add `DbSet<DoctorAppointment> DoctorAppointments`.

**D3 — Run EF migration**
```
dotnet ef migrations add AddDoctorAppointment --project src/VoiceAgent.Infrastructure --startup-project src/VoiceAgent.Api
```

**D4 — Rewrite `HandleDoctorExtras` → `HandleDoctorExtrasAsync`**
- Existing emergency screening and doctor listing logic: keep as-is.
- Add save path: when all required slots are present and state transitions to `AwaitingConfirmation`, prepare a `DoctorAppointment` object. Save it at confirmation (in `HandleConfirmation` or via a dedicated `SaveDoctorAppointmentAsync` method called from `BuildSummaryAndAwaitConfirmation`).
- Include `branch` (clinic location) in the confirmation summary: *"Appointment at {branch} with {doctor} on {day}."*

**D5 — Update `HandleCampaignSpecificAsync`**
- Make `DoctorAppointment` case `await HandleDoctorExtrasAsync(...)`.

**D6 — Fix `BuildFinalResult` for `DoctorAppointment`**
- Add `appointmentId` from the saved entity.
- Keep `status: "CapturedOnly"` (no external booking API yet).

**D7 — Test via demo**
- Go through full appointment flow.
- Say "confirm". Verify `DoctorAppointment` row in DB with all fields populated.
- Verify branch is included in the confirmation message.

---

## Migration Strategy

All four phases touch `ConversationOrchestratorService.cs`. To avoid merge conflicts, implement in **strict phase order** and commit after each phase compiles and passes the demo test.

Migrations timeline:
```
After Phase C:  dotnet ef migrations add AddCabBooking
After Phase D:  dotnet ef migrations add AddDoctorAppointment
```

If phases C and D are developed simultaneously, combine into one migration:
```
dotnet ef migrations add AddCabBookingAndDoctorAppointment
```

---

## Testing Checklist

Each phase is complete when all of these pass via `POST /api/demo/message`:

### Phase A — Courier
- [ ] After weight collected: bot replies with a fare estimate (not just silence)
- [ ] Fare uses band pricing if `CourierDistanceBand` rows exist, flat rate if not
- [ ] "confirm" saves both `CourierQuote` and `CourierOrder` in DB
- [ ] `CourierOrder.CustomerName` and `Phone` are populated

### Phase B — Restaurant
- [ ] Delivery fee matches `ValidationRulesJson`, not hardcoded 3.99
- [ ] Free delivery applies when subtotal exceeds threshold
- [ ] `RestaurantOrder.CustomerName` and `Phone` are populated
- [ ] Itemised cart is spoken before "Does everything look correct?"

### Phase C — Cab
- [ ] After all slots collected: bot speaks a fare quote
- [ ] Airport pickup adds `airportPickupFee` to fare
- [ ] Night-time booking adds night surcharge
- [ ] "confirm" saves `CabBooking` row in DB
- [ ] `BuildFinalResult` fare is not hardcoded

### Phase D — Doctor
- [ ] "confirm" saves `DoctorAppointment` row in DB
- [ ] `DoctorAppointment.ClinicBranch` matches `branch` slot
- [ ] Confirmation message includes branch name
- [ ] `FinalResultJson` includes `appointmentId`

---

## Risk Register

| Risk | Impact | Mitigation |
|------|--------|------------|
| `CourierPricingProfile` seed missing → `null` in dev | Courier confirm crashes | Phase A1 (seed) must be done before A5 (logic) |
| Two EF migrations created simultaneously (Cab + Doctor) | Migration conflict | Combine into one migration or strictly sequence |
| `HandleCabExtrasAsync` geocodes on every message | Slow + extra OSRM calls | Guard: only call OSRM when `estimatedFare` slot is absent |
| `preferredDateTime` free-text can't be reliably parsed for night surcharge (Doctor) | N/A — Doctor doesn't use surcharge | No action needed |
| `RestaurantOrder` cart re-read at confirmation loses deal discount if not in slots | Discount lost | Ensure `slots["discount"]` is persisted via `CollectedSlotsJson` before confirmation |
