# Service Campaigns — Full Change Specification

Every class, entity, migration, seed record, and method change required to complete the four service campaigns.

---

## 1. Courier Service

### 1.1 New Domain Entity — `CourierPricingSettings` (value object in JSON, not a new table)

No new table needed. Urgency fees and fragile fees will be read from the existing `CourierPricingProfile.SettingsJson` column.

**`CourierPricingProfile.SettingsJson` schema to store:**
```json
{
  "sameDayFee": 5.00,
  "fragileFee": 2.00,
  "documentFee": 0.00
}
```

### 1.2 New Seed Record — `CourierPricingProfile`

**File:** `src/VoiceAgent.Infrastructure/Persistence/Seed/CourierSeed.cs` *(new file)*

Add a static `CourierSeed` class alongside `CabSeed` and `DoctorSeed`:

```csharp
public static class CourierSeed
{
    public static readonly CourierPricingProfile PricingProfile = new()
    {
        Id        = Guid.Parse("30000000-0000-0000-0000-000000000201"),
        TenantId  = SeedIds.Tenant,
        ClientId  = SeedIds.CourierClient,
        Name      = "Standard Courier Pricing",
        Currency  = "GBP",
        BaseFee   = 3.50m,
        PricePerKm = 1.20m,
        PricePerKg = 0.50m,
        MinimumFee = 6.00m,
        MaxDistanceKm = 50m,
        SettingsJson = "{\"sameDayFee\":5.00,\"fragileFee\":2.00,\"documentFee\":0.00}",
        IsActive  = true
    };
}
```

**File:** `src/VoiceAgent.Infrastructure/Persistence/Seed/DatabaseSeeder.cs`

Add `CourierSeed.PricingProfile` to the seeder — check if exists before inserting (same pattern as other seeds).

### 1.3 New Domain Entity — `CourierOrder`

`CourierOrder` already exists at `src/VoiceAgent.Domain/Courier/CourierOrder.cs` but is **never written to**. No schema change needed — just needs to be saved.

### 1.4 Changes to `AppDbContext`

**File:** `src/VoiceAgent.Infrastructure/Persistence/AppDbContext.cs`

Verify `CourierDistanceBands` and `CourierWeightBands` are registered as `DbSet<>`. If missing, add:

```csharp
public DbSet<CourierDistanceBand> CourierDistanceBands => Set<CourierDistanceBand>();
public DbSet<CourierWeightBand>   CourierWeightBands   => Set<CourierWeightBand>();
public DbSet<CourierOrders>       CourierOrders        => Set<CourierOrder>();
```

### 1.5 New Private Helper — `CalculateCourierFare`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs`

Add a private static method:

```csharp
private static async Task<decimal> CalculateCourierFareAsync(
    IAppDbContext db,
    CourierPricingProfile profile,
    decimal distanceKm,
    decimal weightKg,
    string packageType,
    string urgency,
    CancellationToken ct)
```

**Logic:**
1. Check `CourierDistanceBand` rows for this profile — find the band containing `distanceKm` and use its `Fee` if present; otherwise fall back to `profile.PricePerKm * distanceKm`.
2. Check `CourierWeightBand` rows — find band for `weightKg` and use its `Fee` if present; otherwise fall back to `profile.PricePerKg * weightKg`.
3. Read `SettingsJson` → deserialise → apply `sameDayFee` if urgency == "same_day", `fragileFee` if packageType == "fragile", `documentFee` if packageType == "document".
4. Return `Math.Max(profile.MinimumFee, profile.BaseFee + distanceFee + weightFee + surcharges)`.

### 1.6 Changes to `HandleCourierExtrasAsync`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs`

**Change 1 — Proactive quote after `weightKg` is answered:**

After `weightKg` is stored in slots and all address slots are present, trigger `ResolveDistanceKmAsync` and `CalculateCourierFareAsync` immediately. Reply with:
> "The distance is approximately {X} km. Based on a {weight} kg {packageType} parcel, your estimated price is {currency}{total}. Shall I confirm this booking?"

Store `distanceKm` and `estimatedFare` in slots so they don't need to be recalculated at confirm.

**Change 2 — Confirm path:**

Read `distanceKm` from slots (pre-calculated) instead of calling OSRM again.
Call `CalculateCourierFareAsync` using pre-stored values.
Save both `CourierQuote` **and** `CourierOrder`:

```csharp
var order = new CourierOrder
{
    Id = Guid.NewGuid(), TenantId = session.TenantId, ClientId = session.ClientId,
    CampaignId = session.CampaignId, CallSessionId = session.Id,
    CourierQuoteId = quote.Id,
    CustomerName = slots.GetValueOrDefault("customerName") ?? string.Empty,
    Phone = slots.GetValueOrDefault("phone") ?? string.Empty,
    FinalResultJson = JsonSerializer.Serialize(final),
    Status = "Confirmed"
};
db.CourierOrders.Add(order);
```

---

## 2. Restaurant Order

### 2.1 `CampaignConfiguration.ValidationRulesJson` — Restaurant Settings Schema

No new entity. Restaurant-specific settings are stored in the existing `ValidationRulesJson` column.

**Schema:**
```json
{
  "deliveryFee": 3.99,
  "taxRatePercent": 0.0,
  "currency": "GBP",
  "freeDeliveryThreshold": 0.0
}
```

**File:** `src/VoiceAgent.Infrastructure/Persistence/Seed/CampaignConfigurationSeed.cs`

Update the Restaurant `CampaignConfiguration` seed record to include `ValidationRulesJson` with the above schema.

### 2.2 New Private Helper — `ParseRestaurantSettings`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs`

```csharp
private sealed class RestaurantSettings
{
    public decimal DeliveryFee          { get; set; } = 3.99m;
    public decimal TaxRatePercent       { get; set; } = 0m;
    public string  Currency             { get; set; } = "GBP";
    public decimal FreeDeliveryThreshold{ get; set; } = 0m;
}

private static RestaurantSettings ParseRestaurantSettings(string? json) { ... }
```

### 2.3 Changes to `HandleRestaurantExtrasAsync`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs`

**Change 1 — Read delivery fee from config:**

Replace `var fee = 3.99m` with:
```csharp
var settings = ParseRestaurantSettings(config?.ValidationRulesJson);
var fee = fulfillmentType == "delivery"
    ? (subtotal >= settings.FreeDeliveryThreshold && settings.FreeDeliveryThreshold > 0 ? 0m : settings.DeliveryFee)
    : 0m;
```

**Change 2 — Tax calculation:**

```csharp
var tax = Math.Round(subtotal * (settings.TaxRatePercent / 100m), 2);
var total = subtotal + fee + tax;
```

**Change 3 — Deal application:**

When user says a deal name (matched from `RestaurantDeal` records), add deal to cart at `DealPrice` rather than individual item prices. Set `slots["appliedDeal"] = deal.Name` and `slots["discount"] = (regularPrice - deal.DealPrice).ToString()`.

**Change 4 — Populate all `RestaurantOrder` fields:**

```csharp
var order = new RestaurantOrder
{
    ...existing fields...
    CustomerName = slots.GetValueOrDefault("customerName") ?? string.Empty,
    Phone        = slots.GetValueOrDefault("phone")        ?? string.Empty,
    Tax          = tax,
    Discount     = decimal.TryParse(slots.GetValueOrDefault("discount"), out var disc) ? disc : 0m,
    Total        = total
};
```

**Change 5 — Itemised cart read-back before `AwaitingConfirmation`:**

In `BuildSummaryAndAwaitConfirmation`, for `RestaurantOrder`, build an itemised line list:
> "Here is your order: 2× Chicken Burger £10.00, 1× Coke £2.50. Subtotal £12.50, delivery £3.99, total £16.49. Does everything look correct?"

This replaces the generic numbered slot summary for restaurant orders.

### 2.4 `MenuItemVariant` Surfacing (Future — Not Phase B)

`MenuItemVariant` integration (size selection) is deferred — the slot model does not currently support per-item variant selection without a richer cart schema.

---

## 3. Cab Booking

### 3.1 New Domain Entity — `CabBooking`

**File:** `src/VoiceAgent.Domain/Cab/CabBooking.cs` *(new file and folder)*

```csharp
namespace VoiceAgent.Domain.Entities;

public class CabBooking
{
    public Guid     Id               { get; set; }
    public Guid     TenantId         { get; set; }
    public Guid     ClientId         { get; set; }
    public Guid     CampaignId       { get; set; }
    public Guid     CallSessionId    { get; set; }
    public string   CustomerName     { get; set; } = string.Empty;
    public string   Phone            { get; set; } = string.Empty;
    public string   PickupLocation   { get; set; } = string.Empty;
    public string   DropoffLocation  { get; set; } = string.Empty;
    public string   PickupDateTime   { get; set; } = string.Empty;
    public int      PassengerCount   { get; set; }
    public string   VehicleType      { get; set; } = string.Empty;
    public decimal  DistanceKm       { get; set; }
    public decimal  EstimatedFare    { get; set; }
    public string   Currency         { get; set; } = string.Empty;
    public bool     NightSurcharge   { get; set; }
    public bool     AirportPickup    { get; set; }
    public string   Status           { get; set; } = string.Empty;   // CapturedOnly / Confirmed
    public DateTime CreatedOn        { get; set; } = DateTime.UtcNow;
}
```

### 3.2 Register in `AppDbContext`

**File:** `src/VoiceAgent.Infrastructure/Persistence/AppDbContext.cs`

```csharp
public DbSet<CabBooking> CabBookings => Set<CabBooking>();
```

### 3.3 EF Migration

Run after entity and DbSet are added:
```
dotnet ef migrations add AddCabBooking --project src/VoiceAgent.Infrastructure --startup-project src/VoiceAgent.Api
```

### 3.4 `CabFareSettings` — Deserialisable from `ValidationRulesJson`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs` (inner class)

```csharp
private sealed class CabFareSettings
{
    public decimal BaseFare              { get; set; } = 3.50m;
    public decimal PricePerKm            { get; set; } = 1.80m;
    public decimal MinimumFare           { get; set; } = 6.00m;
    public decimal NightChargeMultiplier { get; set; } = 1.25m;
    public decimal AirportPickupFee      { get; set; } = 5.00m;
}
```

Nested under `fareSettings` key inside `ValidationRulesJson`:
```json
{
  "fareSettings": { "baseFare": 3.50, "pricePerKm": 1.80, ... },
  "vehicleTypes": [...]
}
```

### 3.5 New Private Helper — `CalculateCabFare`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs`

```csharp
private static decimal CalculateCabFare(
    CabFareSettings settings,
    decimal distanceKm,
    string? pickupDateTime,
    bool isAirport)
```

**Logic:**
1. `base = settings.BaseFare + settings.PricePerKm * distanceKm`
2. If `pickupDateTime` parses to a time between 22:00–06:00 → multiply by `NightChargeMultiplier`.
3. If `isAirport` (pickup or dropoff contains "airport", "heathrow", "gatwick", etc.) → add `AirportPickupFee`.
4. Return `Math.Max(settings.MinimumFare, base)`.

### 3.6 Changes to `HandleCabExtras`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs`

Make async: `private async Task<...?> HandleCabExtrasAsync(...)` — needs `db` and `ct`.

**Change 1 — Proactive fare quote after all slots are present:**

When `pickupLocation`, `dropoffLocation`, `pickupDateTime`, `passengerCount`, `vehicleType` are all in slots and `estimatedFare` is NOT yet in slots:
1. Call `ResolveDistanceKmAsync(pickup, dropoff, ct)`.
2. Parse `fareSettings` from `config.ValidationRulesJson`.
3. Call `CalculateCabFare(...)`.
4. Store `slots["distanceKm"]` and `slots["estimatedFare"]`.
5. Reply: "The estimated fare for your {vehicleType} from {pickup} to {dropoff} is approximately £{fare}. Does that work for you?"

**Change 2 — Confirmation:**

When user says "yes" / "confirm" and `estimatedFare` is in slots:
1. Create and save `CabBooking` entity.
2. Return final result with real fare.
3. Fix `BuildFinalResult` — replace hardcoded `"£18.00"` with `slots.GetValueOrDefault("estimatedFare", "TBC")`.

**Change 3 — Airport detection helper:**

```csharp
private static bool IsAirportAddress(string? address) =>
    address is not null &&
    Regex.IsMatch(address, @"\b(airport|heathrow|gatwick|stansted|luton|manchester airport|lhr|lgw)\b",
        RegexOptions.IgnoreCase);
```

### 3.7 Update `HandleCampaignSpecificAsync`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs`

Change `CabBooking` line from synchronous `HandleCabExtras(lower, slots)` to async:
```csharp
CampaignType.CabBooking => await HandleCabExtrasAsync(session, config, lower, slots, ct),
```

### 3.8 Fix `BuildFinalResult` for `CabBooking`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs`

Replace:
```csharp
estimatedFare = "£18.00"
```
With:
```csharp
estimatedFare = slots.GetValueOrDefault("estimatedFare", "TBC"),
distanceKm    = slots.GetValueOrDefault("distanceKm", "unknown"),
```

---

## 4. Doctor Appointment

### 4.1 New Domain Entity — `DoctorAppointment`

**File:** `src/VoiceAgent.Domain/Doctor/DoctorAppointment.cs` *(new file and folder)*

```csharp
namespace VoiceAgent.Domain.Entities;

public class DoctorAppointment
{
    public Guid     Id                 { get; set; }
    public Guid     TenantId           { get; set; }
    public Guid     ClientId           { get; set; }
    public Guid     CampaignId         { get; set; }
    public Guid     CallSessionId      { get; set; }
    public string   PatientName        { get; set; } = string.Empty;
    public string   Phone              { get; set; } = string.Empty;
    public string   ReasonForVisit     { get; set; } = string.Empty;
    public string   PreferredDoctor    { get; set; } = string.Empty;
    public string   PreferredDateTime  { get; set; } = string.Empty;   // free text as spoken
    public string   ClinicBranch       { get; set; } = string.Empty;
    public string   Status             { get; set; } = string.Empty;   // CapturedOnly / Confirmed / Cancelled
    public string   NotesJson          { get; set; } = string.Empty;
    public DateTime CreatedOn          { get; set; } = DateTime.UtcNow;
}
```

### 4.2 Register in `AppDbContext`

**File:** `src/VoiceAgent.Infrastructure/Persistence/AppDbContext.cs`

```csharp
public DbSet<DoctorAppointment> DoctorAppointments => Set<DoctorAppointment>();
```

### 4.3 EF Migration

Run after entity and DbSet are added:
```
dotnet ef migrations add AddDoctorAppointment --project src/VoiceAgent.Infrastructure --startup-project src/VoiceAgent.Api
```

### 4.4 Changes to `HandleDoctorExtras`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs`

Make async: `private async Task<...?> HandleDoctorExtrasAsync(...)` — needs `db` and `ct`.

**Change 1 — Save appointment at confirmation:**

When all required slots are filled (`patientName`, `phone`, `reasonForVisit`, `preferredDateTime`, `preferredDoctor`) and user transitions to `AwaitingConfirmation` → on confirmation, save:

```csharp
var appointment = new DoctorAppointment
{
    Id               = Guid.NewGuid(),
    TenantId         = session.TenantId,
    ClientId         = session.ClientId,
    CampaignId       = session.CampaignId,
    CallSessionId    = session.Id,
    PatientName      = slots.GetValueOrDefault("patientName")       ?? string.Empty,
    Phone            = slots.GetValueOrDefault("phone")             ?? string.Empty,
    ReasonForVisit   = slots.GetValueOrDefault("reasonForVisit")    ?? string.Empty,
    PreferredDoctor  = slots.GetValueOrDefault("preferredDoctor")   ?? string.Empty,
    PreferredDateTime= slots.GetValueOrDefault("preferredDateTime") ?? string.Empty,
    ClinicBranch     = slots.GetValueOrDefault("branch")            ?? string.Empty,
    Status           = "CapturedOnly"
};
db.DoctorAppointments.Add(appointment);
await db.SaveChangesAsync(ct);
```

**Change 2 — Day-of-week validation on `preferredDateTime`:**

After `preferredDoctor` and `preferredDateTime` are both collected (currently handled partially), also validate that the parsed day matches the doctor's `availableDays`. Already partially implemented — ensure the branch slot is included in the availability message.

**Change 3 — Update `HandleCampaignSpecificAsync`:**

Change from sync to async:
```csharp
CampaignType.DoctorAppointment => await HandleDoctorExtrasAsync(lower, message, slots, session, config, ct),
```

### 4.5 Fix `BuildFinalResult` for `DoctorAppointment`

**File:** `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs`

Add `appointmentId` from saved entity to the final result object so the frontend can reference it.

---

## 5. Shared / Cross-Cutting Changes

### 5.1 `IAppDbContext` — Add missing DbSets

**File:** `src/VoiceAgent.Application/Abstractions/IAppDbContext.cs`

Ensure these are declared (add any missing):
```csharp
DbSet<CourierDistanceBand> CourierDistanceBands { get; }
DbSet<CourierWeightBand>   CourierWeightBands   { get; }
DbSet<CourierOrder>        CourierOrders        { get; }
DbSet<CabBooking>          CabBookings          { get; }
DbSet<DoctorAppointment>   DoctorAppointments   { get; }
```

### 5.2 `DatabaseSeeder.cs` — Add Courier seed

**File:** `src/VoiceAgent.Infrastructure/Persistence/Seed/DatabaseSeeder.cs`

Add `CourierSeed.PricingProfile` upsert logic in `SeedAsync`.

### 5.3 `ConversationOrchestratorService` — Constructor

No constructor change needed. `db` is already injected.

---

## 6. Summary of Files to Create / Modify

| Action | File |
|--------|------|
| **Create** | `src/VoiceAgent.Domain/Cab/CabBooking.cs` |
| **Create** | `src/VoiceAgent.Domain/Doctor/DoctorAppointment.cs` |
| **Create** | `src/VoiceAgent.Infrastructure/Persistence/Seed/CourierSeed.cs` |
| **Modify** | `src/VoiceAgent.Application/Abstractions/IAppDbContext.cs` |
| **Modify** | `src/VoiceAgent.Infrastructure/Persistence/AppDbContext.cs` |
| **Modify** | `src/VoiceAgent.Infrastructure/Persistence/Seed/DatabaseSeeder.cs` |
| **Modify** | `src/VoiceAgent.Infrastructure/Persistence/Seed/CampaignConfigurationSeed.cs` |
| **Modify** | `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs` |
| **Run** | `dotnet ef migrations add AddCabBookingAndDoctorAppointment ...` |

---

## 7. No-Change Decisions

| Item | Reason |
|------|--------|
| `CourierZone` | Zone-based geo-fencing requires polygon intersection logic — deferred |
| `MenuItemVariant` | Variant selection needs per-item cart schema rework — deferred |
| `ICabFareService` / `ICabBookingService` | Interfaces kept but not implemented — logic stays inline in orchestrator for now |
| `IDoctorAvailabilityService` / `IDoctorAppointmentService` | Same — appointment save handled inline |
| Real time-slot availability for Doctor | No time-slot table exists; free-text `preferredDateTime` is validated by day-of-week only |
