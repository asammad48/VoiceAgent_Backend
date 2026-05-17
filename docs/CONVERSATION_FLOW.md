# VoiceAgent — Full Conversation Flow Reference

> Last updated: 2026-05-16  
> Covers all 7 campaign types, the real-time voice pipeline, every external system call,
> happy paths, and every negative / edge-case flow.

---

## Table of Contents

1. [System Architecture at a Glance](#1-system-architecture-at-a-glance)
2. [Real-Time Voice Pipeline (per audio chunk)](#2-real-time-voice-pipeline-per-audio-chunk)
3. [Conversation State Machine](#3-conversation-state-machine)
4. [Generic Question Loop (all campaigns)](#4-generic-question-loop-all-campaigns)
5. [External System Call Map](#5-external-system-call-map)
6. [Campaign Happy Flows](#6-campaign-happy-flows)
   - [6.1 Cab Booking](#61-cab-booking)
   - [6.2 Courier Service](#62-courier-service)
   - [6.3 Restaurant Order](#63-restaurant-order)
   - [6.4 Doctor Appointment](#64-doctor-appointment)
   - [6.5 Medicare Sales](#65-medicare-sales)
   - [6.6 ACA Sales](#66-aca-sales)
   - [6.7 Final Expense (FE) Sales](#67-final-expense-fe-sales)
7. [Negative / Edge-Case Flows](#7-negative--edge-case-flows)
   - [7.1 Empty Transcript — Re-Prompt](#71-empty-transcript--re-prompt)
   - [7.2 Meaningless Response — Re-Ask Inline](#72-meaningless-response--re-ask-inline)
   - [7.3 LLM Finalization Re-Ask](#73-llm-finalization-re-ask)
   - [7.4 User Opt-Out / Not Interested](#74-user-opt-out--not-interested)
   - [7.5 Disqualification (Branch Action)](#75-disqualification-branch-action)
   - [7.6 Graceful Close (Branch Action)](#76-graceful-close-branch-action)
   - [7.7 Abuse — 3-Strike System](#77-abuse--3-strike-system)
   - [7.8 Prompt Injection Attempt](#78-prompt-injection-attempt)
   - [7.9 Call Already Ended (Reconnect Guard)](#79-call-already-ended-reconnect-guard)
   - [7.10 Confirmation Edit — Inline Correction](#710-confirmation-edit--inline-correction)
   - [7.11 Confirmation Edit — Field Reference](#711-confirmation-edit--field-reference)
   - [7.12 Cross-Campaign Redirect](#712-cross-campaign-redirect)
   - [7.13 RAG Override (Knowledge Base Hit)](#713-rag-override-knowledge-base-hit)
   - [7.14 WebSocket Reconnect Mid-Call](#714-websocket-reconnect-mid-call)
8. [Slot Extraction Decision Tree](#8-slot-extraction-decision-tree)
9. [Fare / Quote Calculation Logic](#9-fare--quote-calculation-logic)
10. [Testing Checklist](#10-testing-checklist)

---

## 1. System Architecture at a Glance

```
Browser / Phone
     │
     │  WebSocket (binary PCM audio + text control frames)
     ▼
VoiceStreamOrchestrator          ← handles all real-time audio I/O
     │
     ├── Energy VAD (RMS > 250)  ← no external call — local CPU only
     │
     ├── Deepgram STT ────────────── EXTERNAL: transcribes WAV → text
     │
     ├── ConversationOrchestratorService   ← core state machine
     │       │
     │       ├── RAG retrieval ────────── EXTERNAL: Gemini embedding + pgvector search
     │       ├── Slot extraction ─────── EXTERNAL: Gemini (only when regex fails)
     │       ├── Answer finalization ──── EXTERNAL: Gemini (one call, post-collection)
     │       ├── Location normalization ─ EXTERNAL: Gemini (one call, before geocoding)
     │       ├── Geocoding ───────────── EXTERNAL: Nominatim (one call per address)
     │       └── Routing ─────────────── EXTERNAL: OSRM (one call per booking)
     │
     └── ElevenLabs TTS ──────────── EXTERNAL: text → MP3 audio sent to client
```

---

## 2. Real-Time Voice Pipeline (per audio chunk)

Every ~250 ms the browser sends an 8 236-byte WAV chunk. The server processes each
one synchronously in the WebSocket receive loop:

```
Chunk arrives (8 236 bytes WAV)
        │
        ├─ [Bot is speaking?] ──YES──► Discard chunk (echo suppression)
        │
        NO
        ▼
Compute RMS of PCM samples
        │
        ├─ RMS > 250? ──YES──► Add PCM to accumulator buffer
        │                       Set LastSpeechAt = now
        │                       Set HasSpeech = true
        │
        NO (silent chunk)
        │
        ├─ HasSpeech = false? ──YES──► Discard (silence before any speech)
        │
        HasSpeech = true
        │
        ├─ TrailingSilenceCount < 3? ──YES──► Add chunk to buffer anyway (clean end-of-word)
        │                                      Increment TrailingSilenceCount
        │
        Compute silenceElapsed = now − LastSpeechAt
        Compute speechSec from buffer size
        silenceNeeded = speechSec < 1.5s ? 0.5s : 0.8s
        │
        ├─ silenceElapsed < silenceNeeded? ──YES──► continue (wait for more silence)
        │
        NO — speech turn is complete
        │
        ▼
avgRMS of full buffer < 150? ──YES──► Skip Deepgram (too quiet / empty mic)
        │                              → continue waiting
        NO
        │
        ▼
EXTERNAL CALL 1: Deepgram STT
  Input : WAV (accumulated PCM wrapped in WAV header, 16 kHz mono 16-bit)
  Output: transcript string (may be empty "")
        │
        ├─ transcript == ""? ──YES──►  Re-prompt (speak last bot question again)
        │                              "Sorry, I didn't catch that. <last question>"
        │                              → continue waiting
        │
        NO — valid transcript
        │
        ▼
ConversationOrchestratorService.ProcessMessageAsync(sessionId, transcript)
  [may fire interim TTS mid-call — see §5]
        │
        ▼
EXTERNAL CALL 2: ElevenLabs TTS
  Input : bot reply text
  Output: MP3 binary
        │
        ▼
Send MP3 to client as binary WebSocket frame
Send "bot_ended" control frame
        │
        ├─ ShouldEndCall? ──YES──► Estimate playback delay → wait → send "call_ended"
        │
        NO ──► back to listening
```

---

## 3. Conversation State Machine

```
                         ┌──────────────┐
WebSocket connects ────► │   Started    │
                         └──────┬───────┘
                                │ opening script sent
                         ┌──────▼───────┐
                         │   Greeting   │
                         └──────┬───────┘
                                │ first user message
                         ┌──────▼───────────┐
               ┌────────►│ CollectingSlots  │◄─────────────────────┐
               │         └──────┬────────────┘                     │
               │                │ all slots filled                  │
               │         [LLM Finalization]                         │
               │                │ ambiguous slot found              │
               └────────────────┘ (removes slot, re-asks)          │
                                │                                   │
                                │ AllClear                          │
                         ┌──────▼───────────────┐                  │
                         │  AwaitingConfirmation │                  │
                         └──────┬───────┬────────┘                  │
                       yes      │       │ "change X" / "no"        │
                                │       └────► EditingSlot ─────────┘
                         ┌──────▼───────┐
                         │  Completed   │  ShouldEndCall = true
                         └─────────────┘  EndReason = completed_happy_path

Other terminal states (all set ShouldEndCall = true):
  Declined      ← user opt-out ("not interested") or graceful_close branch
  Disqualified  ← disqualify branch (e.g. age < 65 for Medicare)
  AbuseEnded    ← 3rd abusive message
```

---

## 4. Generic Question Loop (all campaigns)

```
ProcessMessageAsync is called with the user transcript
        │
        ├─ Session already Completed/Declined/Disqualified/AbuseEnded?
        │         → "This call has already ended." ShouldEndCall=true
        │
        ├─ RAG override? (knowledge base returns a hit)
        │         → Reply with RAG answer. Stay in current state.
        │
        ├─ Prompt injection keywords detected?
        │         → Guarded reply. Log event.
        │
        ├─ Abusive language? (3-strike system)
        │         → Warning / end call (see §7.7)
        │
        ├─ State == AwaitingConfirmation?
        │         → HandleConfirmationAsync (see §7.10 / §7.11)
        │
        ├─ State == EditingSlot?
        │         → HandleSlotEditAsync (re-extract, update, re-show summary)
        │
        ├─ Opt-out keywords? ("not interested", "cancel", "stop", etc.)
        │         → Polite closing. State = Declined.
        │
        ├─ Cross-campaign keyword? ("I want to order food" during Cab call)
        │         → Redirect message. Stay in current campaign.
        │
        └─ HandleQuestionnaireAsync
                  │
                  ├─ Current question == null (past last question)?
                  │         → BuildSummaryAndAwaitConfirmationAsync
                  │
                  ├─ TryExtractValue (regex, type-specific, slot-specific)
                  │         EXTERNAL CALL (only if regex fails):
                  │         Gemini slot extraction LLM
                  │
                  ├─ Extracted == null?
                  │         ├─ Disqualification text-match?  → end call
                  │         └─ Re-ask current question
                  │                 "I'm sorry, I didn't quite catch that. <question>"
                  │
                  ├─ Extracted != null
                  │         ├─ Value-based disqualification?  → end call
                  │         ├─ Branch resolution (nextQuestionId, setSlots, action)
                  │         └─ Skip-filled-slots loop
                  │                   (jump over already-answered questions
                  │                    after finalization re-ask)
                  │
                  └─ Next unanswered question?
                            ├─ YES → ask it
                            └─ NO  → BuildSummaryAndAwaitConfirmationAsync
```

### BuildSummaryAndAwaitConfirmationAsync

```
slots["__finalized__"] exists?
        NO → run LLM Finalization
               ▼
        INTERIM TTS: "Please bear with me while I compile your information."
               ▼
        EXTERNAL CALL: Gemini FinalizeAnswersAsync
          Sends all Q&A pairs + slotTypes to Gemini
          Returns: AllClear=true OR list of ambiguous slotIds
               │
               ├─ Ambiguous slot found?
               │         Remove slot, set CurrentQuestionId back
               │         State = CollectingSlots
               │         Reply: "Just to confirm — <question>"
               │         (user answers → skip-loop skips all other filled slots)
               │
               └─ AllClear → set slots["__finalized__"] = "true"

State = AwaitingConfirmation

Campaign == CabBooking?
        ▼
  INTERIM TTS: "Please bear with me while I calculate your fare."
        ▼
  EXTERNAL CALL: Gemini NormalizeLocationsAsync
    Input : raw pickup + dropoff text
    Output: clean geocoding candidates
        ▼
  EXTERNAL CALL: Nominatim geocode(pickup)
  EXTERNAL CALL: Nominatim geocode(dropoff)
        ▼
  EXTERNAL CALL: OSRM route(from, to)  → distanceKm
        ▼
  Fare = baseFare(3.50) + pricePerKm(1.80) × km
       + nightCharge(×1.25 if night)
       + airportFee(5.00 if airport in address)
       (min fare: 6.00 GBP)
        ▼
  Reply: "Your <vehicle> from <pickup> to <dropoff> is approximately <km> km.
          Estimated fare: £<fare>. Shall I confirm your booking?"

Campaign == CourierService?
        ▼
  INTERIM TTS: "Please bear with me while I calculate your quote."
        ▼
  [Same geocoding + routing flow as above]
        ▼
  Fare = distanceBandFee + weightBandFee + urgencyMultiplier(×1.35 if same-day)
       + fragileExtra(2.50) + zoneSurcharge
       (min fee: 7.00 GBP)
        ▼
  Reply: "That's approximately <km> km. Estimated cost for a <weight> kg
          <type> package from <pickup> to <dropoff>: £<fare>.
          Shall I go ahead and confirm the booking?"

All other campaigns:
  BuildNumberedSummary → numbered list of all collected slots
  Reply: "Here are the details I have: 1. name: X; 2. phone: Y; …
          Does everything look correct?"
```

---

## 5. External System Call Map

| # | System | When Called | Input | Output |
|---|--------|-------------|-------|--------|
| 1 | **Deepgram STT** | Every completed speech turn | WAV audio (16 kHz mono) | Transcript string |
| 2 | **ElevenLabs TTS** | After every bot reply + interim messages | Text string | MP3 binary |
| 3 | **Gemini (slot extraction)** | Only when regex extraction returns null | slotId, question, user message, slotType | Extracted value string or null |
| 4 | **Gemini (finalization)** | Once per call, after all slots collected | All Q&A pairs + slot types | AllClear / list of ambiguous slot IDs |
| 5 | **Gemini (location normalization)** | Before Nominatim, for Cab + Courier campaigns | Raw pickup + dropoff text | Clean geocoding candidates |
| 6 | **Nominatim geocoding** | Cab + Courier, during summary build | Address string | Lat/lon coordinate |
| 7 | **OSRM routing** | Cab + Courier, after both addresses geocoded | Two lat/lon pairs | Distance in km |
| 8 | **Gemini (RAG embedding)** | If EnableRag=true and question matches knowledge base | User message | Vector similarity match → answer text |

> **Mock mode:** All external calls 3–8 are replaced by in-memory stubs when
> `FeatureFlags:UseMockProviders: true` (default in Development). Deepgram and
> ElevenLabs are controlled separately via their own API keys.

---

## 6. Campaign Happy Flows

### 6.1 Cab Booking

**Bot identity:** Adam  
**Opening:** "Hi! I'm Adam, your cab booking assistant. I'll get you a quick quote and confirm your ride. Where are we picking you up from?"

```
Q1: "Where should we pick you up?"
    Slot: pickupLocation
    Extraction: StripLocationPrefix → removes "From ", "I'm coming from ", etc.
    Example input : "From Rawalpindi, Pakistan."
    Stored value  : "Rawalpindi, Pakistan"

Q2: "And where are you heading?"
    Slot: dropoffLocation
    Extraction: StripLocationPrefix → removes "I'm heading toward ", "To ", etc.
    Example input : "I'm heading toward Islamabad International Airport."
    Stored value  : "Islamabad International Airport"

Q3: "What date and time do you need the cab?"
    Slot: pickupDateTime  (slotType: datetime)
    Extraction: ExtractDateTimeValue — requires recognisable date/time pattern
    Rejects bare prepositions ("On", "At") — re-asks if no pattern found
    Example input : "Tomorrow at 3 pm"
    Stored value  : "Tomorrow at 3 pm"

Q4: "How many passengers will be travelling?"
    Slot: passengerCount
    Extraction: ExtractNumber

Q5: "What type of vehicle do you prefer — Standard, Executive, 6-Seater,
     or Wheelchair Accessible?"
    Slot: vehicleType
    Valid values: Standard / Executive / 6-Seater / Wheelchair Accessible

Q6: "Can I take your name for the booking?"
    Slot: customerName

Q7: "And your phone number?"
    Slot: phone
    Extraction: ExtractPhone (strips spaces/dashes, validates digit count)

── All slots collected ──────────────────────────────────────────────────

INTERIM TTS: "Please bear with me while I compile your information."
EXTERNAL: Gemini FinalizeAnswersAsync (validates all answers)

INTERIM TTS: "Please bear with me while I calculate your fare."
EXTERNAL: Gemini NormalizeLocationsAsync(pickupLocation, dropoffLocation)
EXTERNAL: Nominatim.geocode(normalizedPickup)
EXTERNAL: Nominatim.geocode(normalizedDropoff)
EXTERNAL: OSRM.route(from, to) → distanceKm

Fare calculation:
  base = 3.50 GBP
  + 1.80 × distanceKm
  + 5.00 if airport in either address
  × 1.25 if pickupDateTime is between 22:00–06:00
  min 6.00 GBP

BOT: "Your Wheelchair Accessible ride from Rawalpindi, Pakistan to
      Islamabad International Airport is approximately 27.6 km.
      Estimated fare: £58.16 (includes airport fee).
      Shall I confirm your booking?"

State: AwaitingConfirmation
─────────────────────────────────────────────────────────────────────────

USER: "Yes"
BOT:  ClosingScript or "All set, <name>! Your cab booking has been captured.
       We'll confirm at <phone> shortly."
State: Completed  ShouldEndCall=true  EndReason=completed_happy_path
```

**Disqualification:** None defined for Cab.  
**Human transfer:** Enabled (`OnlyOnUserRequest`, +441234567890).

---

### 6.2 Courier Service

**Bot identity:** Sam  
**Opening:** "Hi! This is Sam from our courier service. I can help you get a delivery quote and book a pickup. Where are we collecting from today?"

```
Q1: "What is the pickup address?"
    Slot: pickupAddress
    Extraction: StripLocationPrefix

Q2: "And where should we deliver it?"
    Slot: dropoffAddress
    Extraction: StripLocationPrefix

Q3: "What is the approximate weight of the package in kilograms?"
    Slot: weightKg
    Extraction: ExtractNumber

Q4: "Is it a standard parcel, document, or fragile item?"
    Slot: packageType
    Valid values: standard / document / fragile

Q5: "Do you need standard delivery or same-day?"
    Slot: urgency
    Valid values: standard / same_day

Q6: "Can I take your name for the booking?"
    Slot: customerName

Q7: "And your contact number?"
    Slot: phone

── All slots collected ──────────────────────────────────────────────────

INTERIM TTS: "Please bear with me while I compile your information."
EXTERNAL: Gemini FinalizeAnswersAsync

INTERIM TTS: "Please bear with me while I calculate your quote."
EXTERNAL: Gemini NormalizeLocationsAsync(pickupAddress, dropoffAddress)
EXTERNAL: Nominatim.geocode(normalizedPickup)
EXTERNAL: Nominatim.geocode(normalizedDropoff)
EXTERNAL: OSRM.route(from, to) → distanceKm

Fare calculation:
  distanceBandFee:
    0–5 km  → £3     |  5–15 km → £8
    15–30 km → £15   |  30–50 km → £25
  weightBandFee:
    0–2 kg  → £1     |  2–5 kg → £3
  + fragileSurcharge: £2.50 if packageType == fragile
  × urgencyMultiplier: ×1.35 if same_day
  + zoneSurcharge: £0 Bradford / £5 Leeds / £15 Manchester
  min £7.00 GBP

BOT: "That's approximately 27.6 km. Estimated cost for a 2.0 kg standard
      package from Bradford to Leeds: £16.35. Shall I go ahead and confirm?"

State: AwaitingConfirmation
─────────────────────────────────────────────────────────────────────────

USER: "Yes, go ahead"
BOT:  Closing or confirmation message
State: Completed
```

---

### 6.3 Restaurant Order

**Bot identity:** Maya  
**Opening:** "Hi! Welcome to our restaurant. I'm Maya, your ordering assistant. What can I get for you today?"

```
Q1: "What would you like to order? I can tell you about our menu categories or deals."
    Slot: items
    Note: this is a free-text slot for the cart blob (not extracted by regex)
    RAG is enabled — questions about menu items hit the knowledge base first

Q2: "Would you like delivery or pickup?"
    Slot: fulfillmentType
    Valid values: delivery / pickup

Q3: "How would you like to pay — cash or card?"
    Slot: paymentMethod
    Valid values: cash / card

Q4: "Can I take your name for the order?"
    Slot: customerName

Q5: "And your phone number in case we need to reach you?"
    Slot: phone

── All slots collected ──────────────────────────────────────────────────

INTERIM TTS: "Please bear with me while I compile your information."
EXTERNAL: Gemini FinalizeAnswersAsync

No geocoding/routing for Restaurant.

BOT: "Here are the details I have:
      1. items: <cart>; 2. fulfillment: delivery; 3. payment: card;
      4. name: Sarah; 5. phone: 07700900123.
      Does everything look correct?"

State: AwaitingConfirmation
─────────────────────────────────────────────────────────────────────────

USER: "Yes"
State: Completed
```

**Delivery fee:** £3.99 (free over £20 order).  
**RAG:** Enabled — menu FAQ, policy, service info documents.

---

### 6.4 Doctor Appointment

**Bot identity:** Sara  
**Opening:** "Hi, this is Sara from City Health Clinic. I can help you request an appointment. What is the appointment for?"

```
Q1: "What is the reason for your visit?"
    Slot: reasonForVisit (free text)

Q2: "Can I take the patient's full name?"
    Slot: patientName

Q3: "What is the best contact number for the patient?"
    Slot: phone

Q4: "What day and time would you prefer for the appointment?"
    Slot: preferredDateTime  (slotType: datetime)
    Extraction: ExtractDateTimeValue

Q5: "Do you have a preferred doctor, or is any doctor fine?"
    Slot: preferredDoctor  (not required — can skip)

Q6: "Which of our clinic locations is most convenient for you?"
    Slot: branch  (not required — can skip)

── All slots collected ──────────────────────────────────────────────────

INTERIM TTS: "Please bear with me while I compile your information."
EXTERNAL: Gemini FinalizeAnswersAsync

BOT: Numbered summary → "Does everything look correct?"
State: AwaitingConfirmation → Completed
```

**Human transfer:** Enabled (configured in DoctorSeed).

---

### 6.5 Medicare Sales

**Bot identity:** Olivia  
**Opening:** "Hi, this is Olivia calling from Demo Benefits Support. I'm reaching out to see if you'd like information about Medicare-related options that may be available to you. Do you have a few minutes?"

```
Q1: "Great! Are you currently interested in learning about your Medicare options?"
    Slot: interestConfirmed  (yesno)
    Branch: No → graceful_close (bot says goodbye, call ends)
            Yes → Q2

Q2: "Can I get your full name?"
    Slot: leadName

Q3: "Are you currently 65 or older, or approaching 65 soon?"
    Slot: ageRange
    Valid values: 65 or older / approaching 65 / under 65
    Branch: under 65 → DISQUALIFY (not eligible for Medicare)
            * → Q4

Q4: "Do you currently have Medicare Part A or Part B, or any other health coverage?"
    Slot: currentCoverage (free text)

Q5: "What state do you currently live in?"
    Slot: state
    Extraction: ExtractState (matches full US state names + abbreviations)

Q6: "What is the best phone number for a licensed specialist to reach you?"
    Slot: phone

Q7: "And what time works best for a callback — morning, afternoon, or evening?"
    Slot: callbackTime
    Valid values: Morning / Afternoon / Evening

── All slots collected → summary → confirmation → Completed ──────────────
```

**Human transfer:** Enabled (`OnlyOnUserRequest`, +441234567892).

---

### 6.6 ACA Sales

**Bot identity:** Noah  
**Opening:** "Hi, this is Noah from Demo Health Plans. I'm reaching out because you may qualify for a health coverage plan under the Affordable Care Act with reduced premiums. Do you have a few minutes?"

```
Q1: "Great! Are you open to hearing about your health coverage options?"
    Slot: interestConfirmed  Branch: No → graceful_close

Q2: "Can I get your first name?"
    Slot: firstName

Q3: "What state do you currently live in?"
    Slot: state

Q4: "Do you currently have health insurance?"
    Slot: currentInsuranceStatus  (Yes / No)

Q5: "How many people are in your household, including yourself?"
    Slot: householdSize  Extraction: ExtractNumber

Q6: "Roughly what is your annual household income — for example, under $30,000,
     $30k to $60k, or above?"
    Slot: incomeRange  (not required)

Q7: "Are you looking for individual or family coverage?"
    Slot: coverageInterest  (Individual / Family / None)
    Branch: None → graceful_close

Q8: "Do you currently use tobacco products?"
    Slot: tobaccoUse  (Yes / No, not required)

Q9: "What is the best phone number for a licensed agent to reach you?"
    Slot: phone

Q10: "And what time works best — morning, afternoon, or evening?"
     Slot: callbackTime

── All slots collected → summary → confirmation → Completed ──────────────
```

---

### 6.7 Final Expense (FE) Sales

**Bot identity:** Emma  
**Opening:** "Hi, this is Emma from Demo Life Plans. I'm calling about final expense life insurance — a whole-life policy with no medical exam required that helps cover funeral and end-of-life costs so your family is protected. Is this a good time to talk?"

```
Q1: "Great! Are you open to hearing about coverage options?"
    Slot: interestConfirmed  Branch: No → graceful_close

Q2: "Can I start with your first name?"
    Slot: firstName

Q3: "And may I ask your age? Our plans are available for individuals between 50 and 85."
    Slot: age  Extraction: ExtractAge (numeric)
    Disqualification check: outside 50–85 → ends call

Q4: "What state do you currently live in?"
    Slot: state

Q5: "Do you currently smoke or use tobacco products?"
    Slot: tobaccoUse  (Yes / No)
    Branch:
      Yes → Q6a (tobacco version of health question)
      No  → Q6b (clean version of health question)

Q6a (tobacco path):
    "Given that you use tobacco products, have you also been diagnosed with any
     serious health conditions such as cancer, heart disease, or kidney failure
     in the last two years?"
    Slot: healthConditions  (shared slotId between Q6a and Q6b)
    Branch:
      Yes → planType = graded_benefit  → Q7
      No  → planType = graded_standard → Q7

Q6b (clean path):
    "Have you been diagnosed with any serious health conditions such as cancer,
     heart disease, or kidney failure in the last two years?"
    Slot: healthConditions
    Branch:
      Yes → planType = graded_benefit   → Q7
      No  → planType = simplified_issue → Q7

    (planType is set automatically via branch setSlots — never shown to user)

Q7: "How much coverage are you looking for? We offer plans from $5,000 up to $25,000."
    Slot: coverageAmount  Extraction: ExtractCoverageAmount

Q8: "Who would you like listed as the beneficiary on the policy?"
    Slot: beneficiaryName

Q9: "What is the best phone number for a licensed agent to follow up with you?"
    Slot: phone

Q10: "And what time works best for a callback — morning, afternoon, or evening?"
     Slot: callbackTime

── All slots collected → summary → confirmation → Completed ──────────────
```

---

## 7. Negative / Edge-Case Flows

### 7.1 Empty Transcript — Re-Prompt

**Trigger:** Deepgram STT returns `""` (user didn't speak clearly, or background noise accumulated).

```
USER: [mumbles / stays silent]
STT returns: ""
LOG: "Empty transcript after Xs — re-prompting"

BOT: "Sorry, I didn't catch that. <last bot question repeated>"
     → TTS plays immediately via ElevenLabs
     → stays in same state, same question
```

**How to test:**
- Stay silent after the bot asks a question
- Make sounds that aren't words (cough, tap the mic)

---

### 7.2 Meaningless Response — Re-Ask Inline

**Trigger:** STT returns text but regex and LLM slot extraction both return null
(response is too short, is just a filler word, or doesn't match the slot type).

```
BOT:  "What date and time do you need the cab?"
USER: "On."        ← bare preposition rejected by ExtractDateTimeValue
STT:  "On."
Regex: null (no date/time pattern detected)
LLM:  null (Gemini also can't extract a datetime from "On")

BOT: "I'm sorry, I didn't quite catch that. What date and time do you need the cab?"
     → same question repeated with apology prefix
     → state unchanged, waiting for valid answer
```

**How to test:**
- Say "On" / "At" for a date question
- Say "yes" for a location question
- Say "mm" or "uh" for any question

---

### 7.3 LLM Finalization Re-Ask

**Trigger:** After all slots collected, Gemini finalization detects an ambiguous or invalid answer.

```
[All 7 questions answered — attempting to build summary]

INTERIM TTS: "Please bear with me while I compile your information."
EXTERNAL: Gemini FinalizeAnswersAsync
  → Returns: AllClear=false, AmbiguousSlotIds=["pickupDateTime"]

BOT: "Just to confirm — What date and time do you need the cab?"
     State: CollectingSlots, CurrentQuestionId = pickupDateTime

USER: "Tomorrow morning at 9 am"
      Slot extracted → stored
      Skip-loop: all other slots already filled → jump straight to summary

INTERIM TTS: "Please bear with me while I calculate your fare."
[geocoding + routing proceed normally]
BOT: fare quote + confirmation question
```

**Important:** The skip-filled-slots loop means only the flagged question is re-asked.
All other previously answered questions are skipped automatically.

**How to test:**
- For a datetime slot, say "on" or just mumble — finalization should flag it
- For a name slot, say a single letter — finalization should flag it

---

### 7.4 User Opt-Out / Not Interested

**Trigger:** Opt-out keywords in user message at any point during the call.

```
Opt-out keywords: "not interested", "cancel", "stop", "remove me",
                  "don't call", "take me off", "do not call"

BOT: "I completely understand! I'll make sure you're not contacted again.
      Have a wonderful day!"
State: Declined  ShouldEndCall=true  EndReason=user_opt_out
```

**How to test:** Say "I'm not interested" or "please stop" at any point during a question.

---

### 7.5 Disqualification (Branch Action)

**Trigger:** A branch with `action: "disqualify"` matches the extracted value.

```
Example: Medicare — ageRange = "under 65"

BOT: "Based on the information provided, it looks like you may not qualify
      for this program at this time. Thank you for your interest and have
      a great day!"
State: Disqualified  ShouldEndCall=true  EndReason=not_qualified
```

**Campaigns with disqualification branches:**
- Medicare Sales: `ageRange == "under 65"`
- FE Sales: `age` outside 50–85 (checked by `CheckCampaignDisqualificationOnExtract`)
- ACA Sales: `coverageInterest == "None"` → graceful_close (not disqualify)

**How to test:**
- Medicare: say you're "under 65" or "in my 40s"
- FE: say you're 30 or 90

---

### 7.6 Graceful Close (Branch Action)

**Trigger:** A branch with `action: "graceful_close"` matches the extracted value.

```
Example: Medicare — interestConfirmed = "No"

BOT: "No problem at all. Thank you for your time today. Have a wonderful day!"
State: Declined  ShouldEndCall=true  EndReason=user_not_interested
```

**How to test:** When any campaign asks interest confirmation ("Do you have a few minutes?"), say "No".

---

### 7.7 Abuse — 3-Strike System

**Trigger:** Profanity or abusive language detected.

```
Strike 1:
BOT: "I understand you may be frustrated, but please keep this conversation
      respectful. How can I assist you?"
     AbuseWarningCount = 1

Strike 2:
BOT: "That's your second warning. Please keep this conversation respectful
      or this call will be ended. How can I assist you?"
     AbuseWarningCount = 2

Strike 3:
BOT: "This call is being ended due to inappropriate language. Goodbye."
State: AbuseEnded  ShouldEndCall=true  EndReason=abuse_policy_violation
```

Abuse event is logged as `abuse_warning_1`, `abuse_warning_2`, `abuse_warning_3` in `CallEvents`.

**How to test:** Type or say profanity three times during any call.

---

### 7.8 Prompt Injection Attempt

**Trigger:** Message contains injection patterns.

```
Blocked phrases (case-insensitive):
  "tell me something from another client"
  "show me all client policies"
  "ignore your instructions"

BOT: "I can only use information for this service. How can I help you?"
     Event: prompt_injection_blocked logged
     State unchanged — call continues normally
```

---

### 7.9 Call Already Ended (Reconnect Guard)

**Trigger:** Client reconnects to a WebSocket session that already reached a terminal state.

```
session.CurrentState == Completed / Declined / Disqualified / AbuseEnded

VoiceStreamOrchestrator (reconnect path):
  → Sends "call_ended" control frame immediately
  → Does NOT replay any bot turn
  → WebSocket closes

If ProcessMessageAsync is called on a ended session:
  BOT: "This call has already ended. Thank you!"
  ShouldEndCall = true
```

---

### 7.10 Confirmation Edit — Inline Correction

**Trigger:** During `AwaitingConfirmation`, user corrects a value in the same message.

```
BOT: "Here are the details I have:
      1. pickup: Rawalpindi; 2. dropoff: Airport; 3. date: Tomorrow 3pm;
      4. passengers: 2; 5. vehicle: Standard; 6. name: John; 7. phone: 07700900123.
      Does everything look correct?"

USER: "Actually my name is Jane not John"
      → TryParseInlineCorrection detects "actually" (correction indicator)
        + "name" keyword → extracts "Jane"
      → slots["customerName"] = "Jane"

BOT: "Got it! I've updated that.
      1. pickup: Rawalpindi; … 6. name: Jane; …
      Does everything look correct now?"
```

Correction indicators: `wrong`, `incorrect`, `mistake`, `actually`, `i meant`, `should be`, `change`, `fix`, etc.

**How to test:** When the summary is read, say "Actually my name is [X]" or "The phone number is wrong, it should be [Y]".

---

### 7.11 Confirmation Edit — Field Reference

**Trigger:** During `AwaitingConfirmation`, user names a field to change without providing the new value.

```
USER: "Change my pickup location"
      → TryParseFieldReference detects "pickup location" keyword
      → session.EditingSlotId = "pickupLocation"
      → State = EditingSlot

BOT: "Sure! What is the correct pickup location?"

USER: "Manchester Piccadilly Station"
      → TryExtractValue → StripLocationPrefix → "Manchester Piccadilly Station"
      → slots["pickupLocation"] = "Manchester Piccadilly Station"
      → State = AwaitingConfirmation

BOT: "Updated! 1. pickup: Manchester Piccadilly Station; …
      Does everything look correct now?"
```

Users can also reference by number:

```
USER: "Change number 3"   ← refers to 3rd slot in summary
BOT:  "Sure! What is the correct <label for slot 3>?"
```

---

### 7.12 Cross-Campaign Redirect

**Trigger:** User mentions a topic that belongs to a different campaign type.

```
Example: During a Cab Booking call:
USER: "Can I also order some food?"

BOT: "I can help you book a cab. For food orders please contact our
      restaurant service separately."
     → stays in Cab campaign, current question unchanged
```

---

### 7.13 RAG Override (Knowledge Base Hit)

**Trigger:** User asks a question about the service and the knowledge base returns a confident match.

```
(Restaurant campaign, RAG enabled, minScore: 0.72, topK: 4)

USER: "What's in the vegan burger?"
      → RAG retrieval fires before questionnaire engine
      → Gemini embedding of message → pgvector similarity search
      → Hit found (score > 0.72)

BOT: <RAG-generated answer about the vegan burger>
     → State unchanged, CurrentQuestionId unchanged
     → Next user message resumes normal Q&A
```

RAG is **enabled** for Restaurant. All other campaigns have it disabled by default.

---

### 7.14 WebSocket Reconnect Mid-Call

**Trigger:** Client drops and reconnects before the call ends.

```
session.CurrentState == CollectingSlots (or AwaitingConfirmation etc.)

VoiceStreamOrchestrator (reconnect path):
  → Fetches last bot CallTurn from DB
  → Re-speaks it via ElevenLabs TTS
  → "I'm ready — let me repeat: <last question or summary>"
  → Call resumes from exactly where it left off
  → All collected slots are preserved in CollectedSlotsJson
```

If no previous bot turn exists (reconnect at very start): falls back to opening script.

---

## 8. Slot Extraction Decision Tree

For every slot, extraction runs in this order:

```
1. Slot-specific regex (TryExtractValue switch)
   ├─ name slots       → ExtractName (strips titles Mr/Ms, cleans casing)
   ├─ phone            → ExtractPhone (digits only, 10–15 digits)
   ├─ age              → ExtractAge (numeric, 1–110)
   ├─ state            → ExtractState (US state names + abbreviations)
   ├─ number slots     → ExtractNumber (first numeric value in message)
   ├─ yes/no slots     → ExtractYesNo (yes/yeah/yep vs no/nope/nah)
   ├─ location slots   → StripLocationPrefix (strips "From X" / "To X" / "I'm heading toward X")
   │                     then IsMeaningfulResponse check
   ├─ datetime slots   → ExtractDateTimeValue (requires real pattern — day name, month,
   │                     "tomorrow", numeric date, am/pm, time of day word)
   │                     Rejects bare prepositions ("On", "At")
   └─ other            → ExtractBySlotType → uses slotType hint:
                           date/datetime → ExtractDateTimeValue
                           number       → ExtractNumber
                           *            → IsMeaningfulResponse (length ≥ 2, not a filler word)

2. If step 1 returns null:
   EXTERNAL CALL: Gemini slot extraction LLM
   Prompt includes: slotId, question text, user message, slotType hint
   Type-specific instructions added for date/datetime/number/phone/yesno slots

3. If step 2 also returns null:
   → Check campaign disqualification text patterns
   → Re-ask current question with apology prefix
```

**IsMeaningfulResponse** rejects: responses shorter than 2 chars, pure filler words
("yes", "no", "ok", "hmm", "uh", "er"), single letters.

---

## 9. Fare / Quote Calculation Logic

### Cab Fare

```
Settings (from CampaignConfiguration.ValidationRulesJson):
  baseFare:             £3.50
  pricePerKm:           £1.80
  minimumFare:          £6.00
  nightChargeMultiplier: 1.25  (applied if pickup time is 22:00–06:00)
  airportPickupFee:     £5.00  (applied if "airport" in pickup or dropoff address)

Formula:
  fare = baseFare + (pricePerKm × distanceKm)
  if airport: fare += airportPickupFee
  if night:   fare × nightChargeMultiplier
  fare = max(fare, minimumFare)
```

### Courier Fare

```
Settings (from CourierPricingProfile):
  baseFee:     £4.00
  pricePerKm:  £1.10
  pricePerKg:  £0.75
  minimumFee:  £7.00

Distance bands (lookup, overrides pricePerKm):
  0–5 km:   £3     |  5–15 km:  £8
  15–30 km: £15    |  30–50 km: £25

Weight bands (additive):
  0–2 kg: £1    |  2–5 kg: £3

Other surcharges:
  fragile:    +£2.50
  same-day:   × 1.35
  zone fees:  Bradford £0 / Leeds +£5 / Manchester +£15

Formula:
  fare = distanceBandFee + weightBandFee
  if fragile: fare += 2.50
  if same_day: fare × 1.35
  fare += zoneExtraFee
  fare = max(fare, minimumFee)
```

---

## 10. Testing Checklist

### Happy Path Tests

| # | Campaign | Test scenario |
|---|----------|---------------|
| H1 | Cab Booking | Complete all 7 questions, confirm, call ends |
| H2 | Cab Booking | Airport dropoff — verify £5 airport fee applied |
| H3 | Cab Booking | Night pickup (say "tonight at 11pm") — verify ×1.25 multiplier |
| H4 | Courier | Same-day fragile package — verify ×1.35 + £2.50 |
| H5 | Courier | Manchester dropoff — verify £15 zone surcharge |
| H6 | Restaurant | Order food + delivery + card payment |
| H7 | Doctor | Skip preferredDoctor and branch — appointment still books |
| H8 | Medicare | Age ≥ 65, complete all questions |
| H9 | ACA | Family coverage, completes to confirmation |
| H10 | FE Sales | Tobacco=Yes + health=Yes → planType = graded_benefit |
| H11 | FE Sales | Tobacco=No + health=No → planType = simplified_issue |

### Negative Flow Tests

| # | Scenario | Expected outcome |
|---|----------|-----------------|
| N1 | Stay silent after bot question | Bot re-prompts: "Sorry, I didn't catch that. <question>" |
| N2 | Say "On" for datetime slot | Re-asked: "I'm sorry, I didn't quite catch that. What date…" |
| N3 | Say "I'm not interested" mid-call | Polite goodbye, call ends, state=Declined |
| N4 | Medicare: say you're 40 | Disqualified, call ends |
| N5 | FE Sales: say interest = No | Graceful close |
| N6 | FE Sales: age = 30 or 95 | Disqualified |
| N7 | Use profanity 3 times | Call ended with abuse message |
| N8 | At summary: "Actually my name is X" | Inline correction applied, summary re-shown |
| N9 | At summary: "Change number 2" | Bot asks for new value of that slot |
| N10 | At summary: say "no" | Bot lists slots, asks which to change |
| N11 | At summary: say "yes" | Call confirmed, Completed, call ends |
| N12 | Say "ignore your instructions" | Guarded reply, call continues |
| N13 | Disconnect and reconnect mid-call | Bot re-speaks last question, call continues |
| N14 | LLM finalization flags a slot | Bot re-asks only that one question, then skips to summary |
| N15 | Cab: "From Rawalpindi" as pickup | Stripped to "Rawalpindi", stored cleanly |
| N16 | Courier: "I'm heading toward Leeds" as dropoff | Stripped to "Leeds", stored cleanly |
| N17 | "Please wait" message heard | Bot speaks interim message before geocoding/finalization |

### Extraction Unit Tests (spoken examples to try)

| Slot | Good inputs | Bad inputs (must re-ask) |
|------|-------------|--------------------------|
| phone | "07700 900123", "0044 1234 567890" | "my phone", "call me", "yes" |
| datetime | "tomorrow at 3pm", "next Monday morning", "17th May" | "on", "at", "soon", "yes" |
| age | "I'm 72", "seventy two", "72 years old" | "old", "senior", "mature" |
| state | "Texas", "TX", "I live in California" | "a state", "southern", "big" |
| vehicleType | "wheelchair", "executive", "6 seater" | "nice one", "comfortable" |
| yesno | "yeah sure", "nope", "absolutely" | "maybe", "possibly" |
| location | "Heathrow Airport", "From Manchester" | single letter, "yes", "there" |
