# Multi-Intent Campaign Plan

> Status: PLANNING — not yet implemented  
> Date: 2026-05-16

This document covers every change needed to add multiple intents to the 4
service campaigns (Restaurant, Courier, Cab, Doctor). Sales campaigns
(Medicare, ACA, FE) are outbound and stay single-intent.

---

## 1. What We Are Adding

| Campaign | Intents |
|----------|---------|
| **Restaurant** | New Order · Menu/Deal Inquiry · Order Status · Modify/Cancel Order · Complaint · Human Transfer |
| **Courier** | Track Parcel · Book Pickup · Reschedule Delivery · Delivery Complaint · COD/Payment Issue · Human Transfer |
| **Cab** | Book Cab · Fare Estimate · Cancel Ride · Driver Status · Lost Item Complaint · Emergency Transfer |
| **Doctor** | Book Appointment · Reschedule Appointment · Cancel Appointment · Doctor Availability · Fee/Location Inquiry · Emergency Transfer |

---

## 2. Three Intent Flow Types

Every intent belongs to one of three types. The type determines what happens
after intent detection.

### Type A — Collect
> Ask a questionnaire, validate answers, confirm, save result.  
> Examples: Book Cab, Book Pickup, New Order, Book Appointment.  
> **This is the existing flow.** No structural change needed inside it.

### Type B — Lookup
> Ask 1–2 identifying questions (order ref, phone, name), call an
> external/mock API, speak the result, end the call.  
> Examples: Track Parcel, Order Status, Driver Status, Doctor Availability,
> Fare Estimate, Cancel Ride, Reschedule.

### Type C — Transfer
> Speak a bridging message immediately, flag `ShouldEndCall=true` with
> `EndReason=human_transfer`.  
> The frontend dials the transfer number.  
> Examples: Human Transfer (all campaigns), Emergency Transfer (Cab, Doctor).

---

## 3. Changes Required

### 3.1 Database — `CallSession` (1 column)

```
ALTER TABLE "CallSessions"
ADD COLUMN "DetectedIntent" varchar(80) NULL;
```

Add EF Core migration: `AddDetectedIntentToCallSession`.

Current `CallSession.cs` has no intent field.  
We add:
```csharp
public string? DetectedIntent { get; set; }
```

### 3.2 `CampaignConfiguration.QuestionnaireJson` — new shape

Current shape (single questionnaire):
```json
{
  "openingScript": "...",
  "startQuestionId": "...",
  "questions": [ ... ],
  "closingScript": "..."
}
```

New shape (multi-intent):
```json
{
  "openingScript": "Hi! I'm Adam. I can help you book a cab, get a fare estimate, track a ride, or report a lost item. How can I help you today?",
  "intents": [
    {
      "id": "book_cab",
      "name": "Book Cab",
      "type": "collect",
      "triggers": ["book", "new cab", "need a cab", "order", "pick me up"],
      "questionnaire": {
        "startQuestionId": "pickupLocation",
        "closingScript": "...",
        "questions": [ ... ]
      }
    },
    {
      "id": "fare_estimate",
      "name": "Fare Estimate",
      "type": "lookup",
      "triggers": ["how much", "fare", "price", "cost", "estimate", "quote"],
      "questionnaire": {
        "startQuestionId": "pickupLocation",
        "questions": [
          { "id": "pickupLocation", ... },
          { "id": "dropoffLocation", ... }
        ]
      }
    },
    {
      "id": "emergency_transfer",
      "name": "Emergency Transfer",
      "type": "transfer",
      "triggers": ["emergency", "accident", "unsafe", "help me"],
      "transferNumber": "+441234567890",
      "transferMessage": "Connecting you to our emergency line right away. Please stay on the line."
    }
  ]
}
```

**Backward compatibility:** If `intents` key is absent, treat as before (single
questionnaire). All 3 sales campaigns keep their current shape and are unaffected.

### 3.3 New C# models (inside `ConversationOrchestratorService`)

```csharp
// Extend existing QuestionnaireDefinition (already private inner class)
private sealed class QuestionnaireDefinition
{
    [JsonPropertyName("openingScript")] public string? OpeningScript { get; set; }
    [JsonPropertyName("closingScript")]  public string? ClosingScript  { get; set; }

    // Single-intent (legacy) fields
    [JsonPropertyName("startQuestionId")] public string? StartQuestionId { get; set; }
    [JsonPropertyName("questions")]        public List<QuestionDefinition> Questions { get; set; } = [];

    // Multi-intent (new)
    [JsonPropertyName("intents")] public List<IntentDefinition> Intents { get; set; } = [];

    // Helper: are we in multi-intent mode?
    public bool IsMultiIntent => Intents.Count > 0;
}

private sealed class IntentDefinition
{
    [JsonPropertyName("id")]              public string Id             { get; set; } = "";
    [JsonPropertyName("name")]            public string Name           { get; set; } = "";
    [JsonPropertyName("type")]            public string Type           { get; set; } = "collect"; // collect | lookup | transfer
    [JsonPropertyName("triggers")]        public List<string> Triggers { get; set; } = [];
    [JsonPropertyName("questionnaire")]   public QuestionnaireDefinition? Questionnaire { get; set; }
    [JsonPropertyName("transferNumber")]  public string? TransferNumber  { get; set; }
    [JsonPropertyName("transferMessage")] public string? TransferMessage { get; set; }
}
```

### 3.4 New Application Interface — `IIntentDetectionService`

```csharp
// VoiceAgent.Application/Interfaces/IIntentDetectionService.cs
namespace VoiceAgent.Application.Interfaces;

public sealed record IntentMatch(string IntentId, float Confidence);

public interface IIntentDetectionService
{
    /// <summary>
    /// Matches a user utterance against the declared intent triggers.
    /// Tries keyword matching first; falls back to Gemini if confidence is low.
    /// Returns null when no intent can be confidently determined.
    /// </summary>
    Task<IntentMatch?> DetectAsync(
        string userMessage,
        IReadOnlyList<IntentDefinition> intents,
        CancellationToken ct = default);
}
```

### 3.5 Implementations

**`KeywordIntentDetectionService`** (Infrastructure, primary):
- Lowercases message, checks each intent's `triggers` list for substring match
- If exactly one intent matches → return it with confidence 1.0
- If zero or multiple match → return null (falls through to LLM)

**`GeminiIntentDetectionService`** (Infrastructure, fallback):
- Sends the user message + list of intent names/descriptions to Gemini
- Prompt: "The caller said: '...'. Which of these intents best matches? Return only the intent id or UNKNOWN."
- If Gemini returns UNKNOWN → null

**`MockIntentDetectionService`** (Infrastructure, dev/test):
- Always returns the first intent in the list with confidence 1.0
- Controlled by `UseMockProviders` flag

### 3.6 `ConversationOrchestratorService` — routing changes

New block added to `ProcessMessageAsync`, **before** the questionnaire engine:

```
State == Greeting (first user message after opening script)?
        │
        └─ config uses multi-intent?
              YES → DetectIntent(message, intents)
                       │
                       ├─ null (unrecognised) →
                       │    "I can help with [list]. Which would you like?"
                       │    Stay in IntentDetection state
                       │
                       ├─ type == "transfer" →
                       │    Speak transferMessage
                       │    ShouldEndCall=true, EndReason=human_transfer
                       │    session.DetectedIntent = intentId
                       │
                       ├─ type == "lookup" →
                       │    session.DetectedIntent = intentId
                       │    State = CollectingSlots (lookup mini-questionnaire)
                       │
                       └─ type == "collect" →
                            session.DetectedIntent = intentId
                            State = CollectingSlots (full questionnaire)

State == IntentDetection (second attempt after unrecognised)?
        │
        └─ same detection logic
             null again → "I'm sorry, I didn't understand.
                          Let me connect you to a team member." → human transfer
```

When `session.DetectedIntent` is set and state is `CollectingSlots`, the
orchestrator picks the questionnaire from `intents.First(i => i.Id == detectedIntent)
.Questionnaire` instead of the root questionnaire.

**Lookup intent completion** (after mini-questionnaire done):
Instead of calling `BuildSummaryAndAwaitConfirmationAsync`, call
`ExecuteLookupAsync(intentId, slots, ct)` which returns the result message
and sets `ShouldEndCall=true`.

### 3.7 New `ILookupService` interface

```csharp
// VoiceAgent.Application/Interfaces/ILookupService.cs
namespace VoiceAgent.Application.Interfaces;

public interface ILookupService
{
    Task<string> ExecuteAsync(string intentId, Dictionary<string, string> slots, CancellationToken ct = default);
}
```

Each campaign type gets a mock implementation that returns canned data.
Real implementations call actual APIs when integrated.

---

## 4. Intent Definitions per Campaign

### 4.1 Cab — All 6 Intents

| Intent ID | Type | Trigger keywords | Mini-questionnaire slots |
|-----------|------|-----------------|--------------------------|
| `book_cab` | collect | book, ride, cab, pick me up, need a cab, order | All 7 (existing) |
| `fare_estimate` | lookup | how much, fare, price, cost, estimate, quote | pickupLocation, dropoffLocation |
| `cancel_ride` | lookup | cancel, don't need, cancel my cab, cancel ride | bookingRef, phone |
| `driver_status` | lookup | where is, driver, eta, how long, on the way | bookingRef |
| `lost_item` | collect | lost, left, forgot, missing, left something | bookingRef, itemDescription, contactName, phone |
| `emergency_transfer` | transfer | emergency, accident, unsafe, help me, police | — (immediate) |

#### Cab — `fare_estimate` questionnaire (2 questions only)
```
Q1: "Where would you be picked up from?"
    Slot: pickupLocation

Q2: "And where are you heading?"
    Slot: dropoffLocation

── Both collected ──────────────────────────────────────────────
INTERIM TTS: "Please bear with me while I calculate your fare."
[Same NormalizeLocations → Nominatim × 2 → OSRM as book_cab]
Fare calculated using full formula

BOT: "A fare from <pickup> to <dropoff> would be approximately £<fare>.
     This is based on roughly <km> km. Would you like to go ahead and book?"

YES → transition to book_cab intent (re-use existing questionnaire from Q3 onward: datetime, passengers, vehicle, name, phone)
NO  → "No problem! Feel free to call back when you're ready. Goodbye."  ShouldEndCall=true
```

#### Cab — `cancel_ride` questionnaire (2 questions)
```
Q1: "Can I take your booking reference number?"
    Slot: bookingRef

Q2: "And the phone number used for the booking?"
    Slot: phone

── Lookup ──────────────────────────────────────────────────────
LookupService.ExecuteAsync("cancel_ride", slots)
  → Mock: "Your booking <ref> has been cancelled. A refund will be
           processed within 3–5 business days to your original
           payment method."
  → Real: call cab management API

ShouldEndCall=true
```

#### Cab — `driver_status` questionnaire (1 question)
```
Q1: "Can I take your booking reference?"
    Slot: bookingRef

── Lookup ──────────────────────────────────────────────────────
LookupService.ExecuteAsync("driver_status", slots)
  → Mock: "Your driver is approximately 8 minutes away and is currently
           on the A1 heading toward your pickup point."
ShouldEndCall=true
```

#### Cab — `lost_item` questionnaire (4 questions)
```
Q1: "Can I take your booking reference?"
    Slot: bookingRef

Q2: "What item did you leave behind?"
    Slot: itemDescription

Q3: "Can I take your name?"
    Slot: contactName

Q4: "And your best contact number?"
    Slot: phone

── Collect → Confirm → Save ────────────────────────────────────
BOT summary + confirmation → saved as lost_item complaint record
```

#### Cab — `emergency_transfer` (no questionnaire)
```
BOT: "Connecting you to our emergency line right away. Please stay on the line."
ShouldEndCall=true   EndReason=human_transfer
Frontend dials: +441234567890 (same as regular transfer number)
```

---

### 4.2 Courier — All 6 Intents

| Intent ID | Type | Triggers | Mini-questionnaire slots |
|-----------|------|---------|--------------------------|
| `book_pickup` | collect | book, send, parcel, pickup, collect, ship | All 7 (existing) |
| `track_parcel` | lookup | track, where is, tracking, delivery status | trackingNumber |
| `reschedule_delivery` | lookup | reschedule, change delivery, different day, new time | trackingNumber, newDate |
| `delivery_complaint` | collect | complaint, damaged, wrong, late, not delivered, broken | trackingNumber, issueDescription, contactName, phone |
| `cod_payment` | lookup | pay on delivery, COD, payment, cash, how to pay | trackingNumber |
| `human_transfer` | transfer | speak to someone, agent, human, representative | — |

#### Courier — `track_parcel` questionnaire
```
Q1: "Can I take your tracking number?"
    Slot: trackingNumber

── Lookup ──────────────────────────────────────────────────────
LookupService.ExecuteAsync("track_parcel", slots)
  → Mock: "Your parcel <ref> is currently with our delivery driver and
           is estimated to arrive between 2pm and 4pm today."
ShouldEndCall=true
```

#### Courier — `reschedule_delivery` questionnaire
```
Q1: "Can I take your tracking number?"
    Slot: trackingNumber

Q2: "What date would you like us to redeliver? For example, tomorrow or this Friday?"
    Slot: newDate  (slotType: date)
    Extraction: ExtractDateTimeValue

── Lookup ──────────────────────────────────────────────────────
LookupService.ExecuteAsync("reschedule_delivery", slots)
  → Mock: "Done! Your parcel <ref> has been rescheduled for <newDate>.
           You will receive a confirmation text shortly."
ShouldEndCall=true
```

#### Courier — `delivery_complaint` questionnaire
```
Q1: "Can I take your tracking number?"
    Slot: trackingNumber

Q2: "Can you describe the issue? For example, parcel damaged, wrong item, or not received."
    Slot: issueDescription

Q3: "Can I take your name?"
    Slot: contactName

Q4: "And your best contact number?"
    Slot: phone

── Collect → Confirm → Save ────────────────────────────────────
BOT: summary + "Does everything look correct?"
Confirmed → complaint record saved
```

#### Courier — `cod_payment` questionnaire
```
Q1: "Can I take your tracking number?"
    Slot: trackingNumber

── Lookup ──────────────────────────────────────────────────────
LookupService.ExecuteAsync("cod_payment", slots)
  → Mock: "Your parcel <ref> has a cash on delivery payment of £24.50.
           Please have this ready when the driver arrives."
ShouldEndCall=true
```

---

### 4.3 Restaurant — All 6 Intents

| Intent ID | Type | Triggers | Mini-questionnaire slots |
|-----------|------|---------|--------------------------|
| `new_order` | collect | order, food, hungry, menu, pizza, burger, want to order | All 5 (existing) |
| `menu_inquiry` | lookup | what do you have, menu, deals, specials, vegan, allergen | inquiryText |
| `order_status` | lookup | where is, order status, how long, delivery status | orderRef, phone |
| `modify_cancel_order` | lookup | change, modify, cancel order, different item, wrong item | orderRef, phone, changeRequest |
| `complaint` | collect | complaint, cold food, wrong order, bad quality, disgusting | orderRef, complaintDetail, contactName, phone |
| `human_transfer` | transfer | speak to manager, human, agent, someone | — |

#### Restaurant — `menu_inquiry` questionnaire (1 question)
```
Q1: "Of course! What would you like to know about? I can tell you about
     our menu categories, today's deals, or specific dishes."
    Slot: inquiryText (free text, slotType: text)

── Lookup ──────────────────────────────────────────────────────
LookupService.ExecuteAsync("menu_inquiry", slots)
  → This ALSO triggers RAG: the inquiryText is used as a RAG query
    (same as the current knowledge-base hit in new_order flow)
  → Falls back to mock if RAG returns no match:
    "We have a range of options including pizzas, burgers, salads,
     and our daily specials. Would you like to place an order?"

ShouldEndCall=false  (offer to continue to order)
```

Note: After menu inquiry the bot offers to place an order. If user says yes, intent switches to `new_order`.

#### Restaurant — `order_status` questionnaire
```
Q1: "Can I take your order reference or the name on the order?"
    Slot: orderRef

Q2: "And the phone number used for the order?"
    Slot: phone

── Lookup ──────────────────────────────────────────────────────
Mock: "Your order #<ref> has been picked up by our driver and is
       approximately 15 minutes away."
ShouldEndCall=true
```

#### Restaurant — `modify_cancel_order` questionnaire
```
Q1: "Can I take your order reference?"
    Slot: orderRef

Q2: "And your phone number?"
    Slot: phone

Q3: "What would you like to change or cancel?"
    Slot: changeRequest (free text)

── Lookup ──────────────────────────────────────────────────────
Mock: "I've passed your request to the kitchen. Please note that once
       an order is being prepared we may not be able to make changes.
       You'll receive a text confirmation shortly."
ShouldEndCall=true
```

#### Restaurant — `complaint` questionnaire
```
Q1: "Can I take your order reference?"
    Slot: orderRef

Q2: "Can you tell me what the problem was?"
    Slot: complaintDetail

Q3: "And your name?"
    Slot: contactName

Q4: "Best contact number?"
    Slot: phone

── Collect → Confirm → Save ────────────────────────────────────
```

---

### 4.4 Doctor — All 6 Intents

| Intent ID | Type | Triggers | Mini-questionnaire slots |
|-----------|------|---------|--------------------------|
| `book_appointment` | collect | book, appointment, see a doctor, consultation, visit | All 6 (existing) |
| `reschedule_appointment` | lookup | reschedule, change appointment, different time, move | appointmentRef, patientName, newDateTime |
| `cancel_appointment` | lookup | cancel, cancel appointment, don't need | appointmentRef, patientName |
| `doctor_availability` | lookup | available, availability, when can I see, which doctor, today | specialty, preferredDate |
| `fee_location_inquiry` | lookup | how much, fee, cost, address, where, location, parking | inquiryText |
| `emergency_transfer` | transfer | emergency, chest pain, can't breathe, urgent help | — |

#### Doctor — `reschedule_appointment` questionnaire
```
Q1: "Can I take your appointment reference or the patient's name?"
    Slot: appointmentRef

Q2: "And the patient's name?"
    Slot: patientName

Q3: "What date and time would you prefer instead?"
    Slot: newDateTime  (slotType: datetime)

── Lookup ──────────────────────────────────────────────────────
Mock: "Your appointment for <patientName> has been rescheduled to
       <newDateTime>. A confirmation text will be sent shortly."
ShouldEndCall=true
```

#### Doctor — `cancel_appointment` questionnaire
```
Q1: "Can I take your appointment reference or the patient's name?"
    Slot: appointmentRef

Q2: "And the patient's name to confirm?"
    Slot: patientName

── Lookup ──────────────────────────────────────────────────────
Mock: "The appointment for <patientName> has been successfully cancelled.
       If you'd like to rebook in the future, please give us a call."
ShouldEndCall=true
```

#### Doctor — `doctor_availability` questionnaire
```
Q1: "What type of appointment are you looking for? For example, GP, dermatology, or physiotherapy."
    Slot: specialty

Q2: "What date were you hoping for?"
    Slot: preferredDate  (slotType: date)

── Lookup ──────────────────────────────────────────────────────
Mock: "We have availability with Dr. Ahmed in general practice on
       <date> at 10:00 am, 2:00 pm, and 4:30 pm.
       Would you like to book one of these slots?"

ShouldEndCall=false  (offer to book)
If user says yes → switch to book_appointment intent
```

#### Doctor — `fee_location_inquiry` questionnaire
```
Q1: "Of course. What would you like to know? For example, our consultation fee,
     address, opening hours, or parking."
    Slot: inquiryText

── Lookup ──────────────────────────────────────────────────────
→ RAG query using inquiryText (same knowledge-base pipeline)
→ Mock fallback:
  "Our standard consultation fee is £60. We are located at
   12 Health Street, London. Opening hours are Monday to Friday 8am–6pm,
   Saturday 9am–1pm. Paid parking is available on site."
ShouldEndCall=true
```

#### Doctor — `emergency_transfer` (no questionnaire)
```
BOT: "This sounds urgent. Let me connect you to our emergency line immediately.
      Please stay on the line."
ShouldEndCall=true   EndReason=human_transfer
```

---

## 5. Full Revised State Machine

```
WebSocket connects
        │
        ▼ opening script played
    Greeting
        │
        │ first user message
        ▼
─ Multi-intent campaign? ──NO──► [existing single-intent flow unchanged]
        │
       YES
        ▼
    IntentDetection
        │
        ├── Keyword match → intent found
        │        │
        │        ├─ type == "transfer"
        │        │       → speak transferMessage
        │        │       → ShouldEndCall=true, EndReason=human_transfer
        │        │
        │        ├─ type == "lookup"
        │        │       → session.DetectedIntent = id
        │        │       → state = CollectingSlots
        │        │       → mini-questionnaire for this intent
        │        │
        │        └─ type == "collect"
        │                → session.DetectedIntent = id
        │                → state = CollectingSlots
        │                → full questionnaire for this intent
        │
        └── No match / ambiguous
                 → "I can help with [list intents]. Which would you like?"
                 → state = IntentDetection (second attempt)
                 → second failure → immediate human transfer

    CollectingSlots  [same as existing]
        │
        │ [lookup intent path]
        ▼
    ExecuteLookupAsync
        │
        ├─ intent offers to continue (menu_inquiry, doctor_availability, fare_estimate)
        │       → stay active, wait for yes/no
        │       → yes: transition to booking intent for this campaign
        │       → no: goodbye, ShouldEndCall=true
        │
        └─ one-shot lookup (track, status, cancel, reschedule, COD)
                → speak result
                → ShouldEndCall=true
                → EndReason=lookup_completed

        │ [collect intent path — same as today]
        ▼
    AwaitingConfirmation → Completed
```

---

## 6. Opening Scripts (Updated for Multi-Intent)

### Cab
```
"Hi! I'm Adam, your cab assistant. I can help you book a ride, get a
 fare estimate, cancel a booking, check on your driver, or report a
 lost item. How can I help you today?"
```

### Courier
```
"Hi! This is Sam from our courier service. I can help you book a
 pickup, track a parcel, reschedule a delivery, or raise a complaint.
 What can I do for you?"
```

### Restaurant
```
"Hi! Welcome. I'm Maya. I can take a new order, tell you about our
 menu and deals, check your order status, or help with a complaint.
 What would you like?"
```

### Doctor
```
"Hi, this is Sara from City Health Clinic. I can help you book or
 manage an appointment, check doctor availability, or answer questions
 about fees and our location. How can I help?"
```

---

## 7. Implementation Order

### Step 1 — Schema & Model (1 migration, 1 class change)
- Add `CallSession.DetectedIntent` column → EF migration
- Add `IntentDefinition` class to orchestrator
- Extend `QuestionnaireDefinition` with `Intents` list

### Step 2 — Intent Detection Service
- `IIntentDetectionService` interface
- `KeywordIntentDetectionService` (regex/contains, no external call)
- `GeminiIntentDetectionService` (LLM fallback)
- `MockIntentDetectionService` (dev)
- Register in `DependencyInjection.cs`

### Step 3 — Lookup Service
- `ILookupService` interface
- `MockLookupService` (all intents, canned responses)
- Register in DI

### Step 4 — Orchestrator Routing
- Add intent detection block to `ProcessMessageAsync`
- Add `HandleIntentDetectionAsync` method
- Add `ExecuteLookupAsync` method
- Route `CollectingSlots` to correct sub-questionnaire based on `DetectedIntent`

### Step 5 — Questionnaire JSON Updates
- Rewrite all 4 seed configurations with new multi-intent structure
- New `CampaignConfigurationSeed.cs` entries
- Update opening scripts

### Step 6 — Continue-to-Book Transitions
- After `menu_inquiry`, `doctor_availability`, `fare_estimate` — offer to book
- If user says "yes": switch `DetectedIntent` to booking intent, set `CurrentQuestionId` to first unanswered booking question

### Step 7 — Human Transfer Handling
- `type: transfer` intents already return `ShouldEndCall=true`
- Frontend must handle `EndReason=human_transfer` by dialling `transferNumber`
- Confirm existing `HumanTransferJson` on CampaignConfiguration is read by frontend

---

## 8. What Does NOT Change

- `ConversationState` enum — `IntentDetection` already exists, no new states needed
- All 3 sales campaigns (Medicare, ACA, FE) — single-intent, untouched
- Slot extraction logic, LLM finalization, location normalization — all unchanged
- Voice pipeline (Deepgram, ElevenLabs, VAD) — untouched
- Re-prompt on empty transcript — applies to all intents automatically
- Abuse detection, opt-out, RAG — all still apply across all intents
- Existing `book_*` / `new_order` questionnaire questions — unchanged (just nested under intent)

---

## 9. Mock Lookup Responses Reference

For local dev (`UseMockProviders=true`), `MockLookupService` returns:

| Intent | Mock response template |
|--------|----------------------|
| `track_parcel` | "Your parcel {trackingNumber} is with our driver, estimated delivery 2pm–4pm today." |
| `order_status` | "Your order {orderRef} has been picked up and is about 15 minutes away." |
| `cancel_ride` | "Booking {bookingRef} has been cancelled. Refund in 3–5 business days." |
| `driver_status` | "Your driver is approximately 8 minutes away." |
| `reschedule_delivery` | "Parcel {trackingNumber} rescheduled for {newDate}. Confirmation text sent." |
| `reschedule_appointment` | "Appointment for {patientName} moved to {newDateTime}. Text sent." |
| `cancel_appointment` | "Appointment for {patientName} cancelled successfully." |
| `doctor_availability` | "Dr. Ahmed (GP) is available on {preferredDate} at 10am, 2pm, 4:30pm." |
| `cod_payment` | "COD amount for {trackingNumber} is £24.50. Please have it ready." |
| `modify_cancel_order` | "Change request for {orderRef} sent to kitchen. Text confirmation coming." |
| `fee_location_inquiry` | "Consultation fee £60. Address: 12 Health Street. Hours: Mon–Fri 8am–6pm." |
| `menu_inquiry` | RAG first, fallback: "We have pizzas, burgers, salads and daily specials." |

---

## 10. Testing the New Intents

### Cab — Test phrases per intent

| Intent | Say this |
|--------|----------|
| book_cab | "I need a cab to the airport" |
| fare_estimate | "How much would it cost from Manchester to Leeds?" |
| cancel_ride | "I need to cancel my booking" |
| driver_status | "Where is my driver?" |
| lost_item | "I think I left my bag in the cab" |
| emergency_transfer | "There's been an accident" |

### Courier — Test phrases

| Intent | Say this |
|--------|----------|
| book_pickup | "I need to send a parcel" |
| track_parcel | "Where is my parcel?" |
| reschedule_delivery | "Can I get my delivery on Friday instead?" |
| delivery_complaint | "My parcel arrived damaged" |
| cod_payment | "How do I pay on delivery?" |
| human_transfer | "I need to speak to someone" |

### Restaurant — Test phrases

| Intent | Say this |
|--------|----------|
| new_order | "I'd like to order a pizza" |
| menu_inquiry | "What vegan options do you have?" |
| order_status | "Where is my order?" |
| modify_cancel_order | "I want to cancel my order" |
| complaint | "My food arrived cold" |
| human_transfer | "Let me speak to the manager" |

### Doctor — Test phrases

| Intent | Say this |
|--------|----------|
| book_appointment | "I need to see a doctor" |
| reschedule_appointment | "I need to move my appointment" |
| cancel_appointment | "Cancel my appointment please" |
| doctor_availability | "Is there anything available on Thursday?" |
| fee_location_inquiry | "How much is a consultation?" |
| emergency_transfer | "I'm having chest pains" |

### Ambiguous intent — Test phrase
- Say "I have a question" → bot should list options and ask which
- Say nothing meaningful twice → bot should connect to human transfer
