using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class CampaignConfigurationSeed
{
    // ── Questionnaire JSON constants ──────────────────────────────────────────

    private const string RestaurantQuestionnaire = """
        {
          "openingScript": "Hi! Welcome. I'm Maya. I can take a new order, tell you about our menu and deals, check or modify your order status, or help with a complaint. What would you like?",
          "intents": [
            {
              "id": "new_order", "name": "New Order", "type": "collect",
              "triggers": ["order","food","hungry","pizza","burger","want to order","place an order","i'd like to","i want to"],
              "questionnaire": {
                "startQuestionId": "items",
                "closingScript": "Your order has been placed. Thank you!",
                "questions": [
                  { "id": "items",           "order": 1, "nextQuestionId": "fulfillmentType", "question": "What would you like to order? I can tell you about our menu or deals.", "required": true },
                  { "id": "fulfillmentType", "order": 2, "nextQuestionId": "paymentMethod",   "question": "Would you like delivery or pickup?", "required": true, "validValues": ["delivery","pickup"] },
                  { "id": "paymentMethod",   "order": 3, "nextQuestionId": "customerName",    "question": "How would you like to pay — cash or card?", "required": true, "validValues": ["cash","card"] },
                  { "id": "customerName",    "order": 4, "nextQuestionId": "phone",           "question": "Can I take your name for the order?", "required": true },
                  { "id": "phone",           "order": 5,                                      "question": "And your phone number in case we need to reach you?", "required": true }
                ]
              }
            },
            {
              "id": "menu_inquiry", "name": "Menu Inquiry", "type": "lookup",
              "triggers": ["menu","deals","what do you have","specials","vegan","allergen","categories","what's available","options"],
              "continueToIntentId": "new_order",
              "questionnaire": {
                "startQuestionId": "inquiryText",
                "questions": [
                  { "id": "inquiryText", "order": 1, "question": "Of course! What would you like to know about? I can tell you about menu categories, today's deals, or specific dishes.", "required": true }
                ]
              }
            },
            {
              "id": "order_status", "name": "Order Status", "type": "lookup",
              "triggers": ["where is my order","order status","how long","my order","track order","delivery status"],
              "questionnaire": {
                "startQuestionId": "orderRef",
                "questions": [
                  { "id": "orderRef", "order": 1, "nextQuestionId": "phone", "question": "Can I take your order reference or the name on the order?", "required": true },
                  { "id": "phone",    "order": 2,                             "question": "And the phone number used for the order?", "required": true }
                ]
              }
            },
            {
              "id": "modify_cancel_order", "name": "Modify or Cancel Order", "type": "lookup",
              "triggers": ["change","modify","cancel order","different item","wrong item","cancel my order"],
              "questionnaire": {
                "startQuestionId": "orderRef",
                "questions": [
                  { "id": "orderRef",      "order": 1, "nextQuestionId": "phone",         "question": "Can I take your order reference?", "required": true },
                  { "id": "phone",         "order": 2, "nextQuestionId": "changeRequest", "question": "And your phone number?", "required": true },
                  { "id": "changeRequest", "order": 3,                                    "question": "What would you like to change or cancel?", "required": true }
                ]
              }
            },
            {
              "id": "complaint", "name": "Complaint", "type": "collect",
              "triggers": ["complaint","cold food","wrong order","bad quality","disgusting","missing item","late delivery"],
              "questionnaire": {
                "startQuestionId": "orderRef",
                "closingScript": "Thank you! Your complaint has been recorded and our team will be in touch with you shortly to resolve this.",
                "questions": [
                  { "id": "orderRef",       "order": 1, "nextQuestionId": "complaintDetail", "question": "Can I take your order reference?", "required": true },
                  { "id": "complaintDetail","order": 2, "nextQuestionId": "customerName",     "question": "Can you tell me what the problem was?", "required": true },
                  { "id": "customerName",   "order": 3, "nextQuestionId": "phone",           "question": "And your name?", "required": true },
                  { "id": "phone",          "order": 4,                                      "question": "Best contact number?", "required": true }
                ]
              }
            },
            {
              "id": "human_transfer", "name": "Speak to Someone", "type": "transfer",
              "triggers": ["speak to manager","human","agent","someone","representative","real person"],
              "transferMessage": "Of course! Let me connect you to a team member. Please hold."
            }
          ]
        }
        """;

    private const string CourierQuestionnaire = """
        {
          "openingScript": "Hi! This is Sam from our courier service. I can help you book a pickup, track a parcel, reschedule a delivery, modify or cancel an order, or raise a complaint. What can I do for you?",
          "intents": [
            {
              "id": "book_pickup", "name": "Book Pickup", "type": "collect",
              "triggers": ["book","send","parcel","pickup","collect","ship","new booking","send a package"],
              "questionnaire": {
                "startQuestionId": "pickupAddress",
                "questions": [
                  { "id": "pickupAddress",  "order": 1, "nextQuestionId": "dropoffAddress", "question": "What is the pickup address?", "required": true },
                  { "id": "dropoffAddress", "order": 2, "nextQuestionId": "weightKg",       "question": "And where should we deliver it?", "required": true },
                  { "id": "weightKg",       "order": 3, "nextQuestionId": "packageType",    "question": "What is the approximate weight of the package in kilograms?", "required": true },
                  { "id": "packageType",    "order": 4, "nextQuestionId": "urgency",        "question": "Is it a standard parcel, document, or fragile item?", "required": true, "validValues": ["standard","document","fragile"] },
                  { "id": "urgency",        "order": 5, "nextQuestionId": "customerName",   "question": "Do you need standard delivery or same-day?", "required": true, "validValues": ["standard","same_day"] },
                  { "id": "customerName",   "order": 6, "nextQuestionId": "phone",          "question": "Can I take your name for the booking?", "required": true },
                  { "id": "phone",          "order": 7,                                     "question": "And your contact number?", "required": true }
                ]
              }
            },
            {
              "id": "track_parcel", "name": "Track Parcel", "type": "lookup",
              "triggers": ["track","where is","tracking","delivery status","track my","where is my parcel"],
              "questionnaire": {
                "startQuestionId": "trackingNumber",
                "questions": [
                  { "id": "trackingNumber", "order": 1, "question": "Can I take your tracking number?", "required": true }
                ]
              }
            },
            {
              "id": "reschedule_delivery", "name": "Reschedule Delivery", "type": "lookup",
              "triggers": ["reschedule","change delivery","different day","new time","redeliver","change my delivery"],
              "questionnaire": {
                "startQuestionId": "trackingNumber",
                "questions": [
                  { "id": "trackingNumber", "order": 1, "nextQuestionId": "newDate", "question": "Can I take your tracking number?", "required": true },
                  { "id": "newDate", "order": 2, "slotType": "date", "question": "What date would you like us to redeliver? For example, tomorrow or this Friday?", "required": true }
                ]
              }
            },
            {
              "id": "delivery_complaint", "name": "Delivery Complaint", "type": "collect",
              "triggers": ["complaint","damaged","wrong","late","not delivered","broken","missing"],
              "questionnaire": {
                "startQuestionId": "trackingNumber",
                "closingScript": "Thank you! Your complaint has been logged and our team will contact you shortly to resolve this.",
                "questions": [
                  { "id": "trackingNumber",    "order": 1, "nextQuestionId": "issueDescription", "question": "Can I take your tracking number?", "required": true },
                  { "id": "issueDescription",  "order": 2, "nextQuestionId": "customerName",     "question": "Can you describe the issue? For example, parcel damaged, wrong item, or not received.", "required": true },
                  { "id": "customerName",      "order": 3, "nextQuestionId": "phone",            "question": "Can I take your name?", "required": true },
                  { "id": "phone",             "order": 4,                                       "question": "And your best contact number?", "required": true }
                ]
              }
            },
            {
              "id": "cod_payment", "name": "COD Payment", "type": "lookup",
              "triggers": ["pay on delivery","cod","payment","cash","how to pay","cash on delivery"],
              "questionnaire": {
                "startQuestionId": "trackingNumber",
                "questions": [
                  { "id": "trackingNumber", "order": 1, "question": "Can I take your tracking number?", "required": true }
                ]
              }
            },
            {
              "id": "modify_order", "name": "Modify Order", "type": "lookup",
              "triggers": ["change address","update address","change pickup","change destination","modify order","update delivery address","change drop","wrong address","change my order"],
              "questionnaire": {
                "startQuestionId": "trackingNumber",
                "questions": [
                  { "id": "trackingNumber", "order": 1, "nextQuestionId": "changeRequest", "question": "Can I take your tracking or order reference number?", "required": true },
                  { "id": "changeRequest",  "order": 2,                                    "question": "What would you like to change? For example, the pickup address, destination, or delivery date.", "required": true }
                ]
              }
            },
            {
              "id": "cancel_order", "name": "Cancel Order", "type": "lookup",
              "triggers": ["cancel order","cancel my order","cancel delivery","cancel parcel","cancel pickup","cancel my booking","cancel this order"],
              "questionnaire": {
                "startQuestionId": "trackingNumber",
                "questions": [
                  { "id": "trackingNumber", "order": 1, "nextQuestionId": "customerName", "question": "Can I take your tracking or order reference number?", "required": true },
                  { "id": "customerName",   "order": 2,                                   "question": "And your name to confirm the cancellation?", "required": true }
                ]
              }
            },
            {
              "id": "human_transfer", "name": "Speak to Someone", "type": "transfer",
              "triggers": ["speak to someone","agent","human","representative","real person","speak to a person"],
              "transferMessage": "Of course! Let me connect you to a team member. Please hold."
            }
          ]
        }
        """;

    private const string CabQuestionnaire = """
        {
          "openingScript": "Hi! I'm Adam, your cab assistant. I can help you book a ride, get a fare estimate, cancel or modify a booking, check on your driver, or report a lost item. How can I help you today?",
          "intents": [
            {
              "id": "book_cab", "name": "Book Cab", "type": "collect",
              "triggers": ["book","ride","cab","pick me up","need a cab","order a cab","taxi","i need a ride"],
              "questionnaire": {
                "startQuestionId": "pickupLocation",
                "questions": [
                  { "id": "pickupLocation",  "order": 1, "nextQuestionId": "dropoffLocation", "question": "Where should we pick you up?", "required": true },
                  { "id": "dropoffLocation", "order": 2, "nextQuestionId": "pickupDateTime",  "question": "And where are you heading?", "required": true },
                  { "id": "pickupDateTime",  "order": 3, "nextQuestionId": "passengerCount",  "question": "What date and time do you need the cab?", "required": true },
                  { "id": "passengerCount",  "order": 4, "nextQuestionId": "vehicleType",     "question": "How many passengers will be travelling?", "required": true },
                  { "id": "vehicleType",     "order": 5, "nextQuestionId": "customerName",    "question": "What type of vehicle do you prefer — Standard, Executive, 6-Seater, or Wheelchair Accessible?", "required": true, "validValues": ["standard","executive","6-seater","wheelchair accessible"] },
                  { "id": "customerName",    "order": 6, "nextQuestionId": "phone",           "question": "Can I take your name for the booking?", "required": true },
                  { "id": "phone",           "order": 7,                                      "question": "And your phone number?", "required": true }
                ]
              }
            },
            {
              "id": "fare_estimate", "name": "Fare Estimate", "type": "lookup",
              "triggers": ["how much","fare","price","cost","estimate","quote","what would it cost","how much would it be"],
              "continueToIntentId": "book_cab",
              "questionnaire": {
                "startQuestionId": "pickupLocation",
                "questions": [
                  { "id": "pickupLocation",  "order": 1, "nextQuestionId": "dropoffLocation", "question": "Where would you be picked up from?", "required": true },
                  { "id": "dropoffLocation", "order": 2,                                      "question": "And where are you heading?", "required": true }
                ]
              }
            },
            {
              "id": "cancel_ride", "name": "Cancel Ride", "type": "lookup",
              "triggers": ["cancel","don't need","cancel my cab","cancel ride","cancel booking","cancel my booking"],
              "questionnaire": {
                "startQuestionId": "bookingRef",
                "questions": [
                  { "id": "bookingRef", "order": 1, "nextQuestionId": "phone", "question": "Can I take your booking reference number?", "required": true },
                  { "id": "phone",      "order": 2,                             "question": "And the phone number used for the booking?", "required": true }
                ]
              }
            },
            {
              "id": "driver_status", "name": "Driver Status", "type": "lookup",
              "triggers": ["where is","driver","eta","how long","on the way","where is my driver","driver status"],
              "questionnaire": {
                "startQuestionId": "bookingRef",
                "questions": [
                  { "id": "bookingRef", "order": 1, "question": "Can I take your booking reference?", "required": true }
                ]
              }
            },
            {
              "id": "lost_item", "name": "Lost Item", "type": "collect",
              "triggers": ["lost","left","forgot","missing","left something","left my","i think i left"],
              "questionnaire": {
                "startQuestionId": "bookingRef",
                "closingScript": "Thank you! We've logged your lost item report and our driver team will investigate and contact you shortly.",
                "questions": [
                  { "id": "bookingRef",      "order": 1, "nextQuestionId": "itemDescription", "question": "Can I take your booking reference?", "required": true },
                  { "id": "itemDescription", "order": 2, "nextQuestionId": "customerName",    "question": "What item did you leave behind?", "required": true },
                  { "id": "customerName",    "order": 3, "nextQuestionId": "phone",           "question": "Can I take your name?", "required": true },
                  { "id": "phone",           "order": 4,                                      "question": "And your best contact number?", "required": true }
                ]
              }
            },
            {
              "id": "modify_ride", "name": "Modify Ride", "type": "lookup",
              "triggers": ["change destination","change drop","update booking","modify booking","change my booking","change dropoff","different destination","wrong destination","change my ride"],
              "questionnaire": {
                "startQuestionId": "bookingRef",
                "questions": [
                  { "id": "bookingRef",    "order": 1, "nextQuestionId": "changeRequest", "question": "Can I take your booking reference number?", "required": true },
                  { "id": "changeRequest", "order": 2,                                    "question": "What would you like to change? For example, the destination, pickup time, or vehicle type.", "required": true }
                ]
              }
            },
            {
              "id": "emergency_transfer", "name": "Emergency", "type": "transfer",
              "triggers": ["emergency","accident","unsafe","help me","police","danger","injured"],
              "transferMessage": "Connecting you to our emergency line right away. Please stay on the line."
            }
          ]
        }
        """;

    private const string DoctorQuestionnaire = """
        {
          "openingScript": "Hi, this is Sara from City Health Clinic. I can help you book or manage an appointment, check doctor availability, or answer questions about fees and our location. How can I help?",
          "intents": [
            {
              "id": "book_appointment", "name": "Book Appointment", "type": "collect",
              "triggers": ["book","appointment","see a doctor","consultation","visit","i need to see","schedule","i'd like to book"],
              "questionnaire": {
                "startQuestionId": "reasonForVisit",
                "questions": [
                  { "id": "reasonForVisit",    "order": 1, "nextQuestionId": "patientName",       "question": "What is the reason for your visit?", "required": true },
                  { "id": "patientName",       "order": 2, "nextQuestionId": "phone",             "question": "Can I take the patient's full name?", "required": true },
                  { "id": "phone",             "order": 3, "nextQuestionId": "preferredDateTime", "question": "What is the best contact number for the patient?", "required": true },
                  { "id": "preferredDateTime", "order": 4, "nextQuestionId": "preferredDoctor",   "question": "What day and time would you prefer for the appointment?", "required": true },
                  { "id": "preferredDoctor",   "order": 5, "nextQuestionId": "branch",            "question": "Do you have a preferred doctor, or is any doctor fine?", "required": false },
                  { "id": "branch",            "order": 6,                                        "question": "Which of our clinic locations is most convenient for you?", "required": false }
                ]
              }
            },
            {
              "id": "reschedule_appointment", "name": "Reschedule Appointment", "type": "lookup",
              "triggers": ["reschedule","change appointment","different time","move my appointment","change my appointment"],
              "questionnaire": {
                "startQuestionId": "appointmentRef",
                "questions": [
                  { "id": "appointmentRef", "order": 1, "nextQuestionId": "patientName",  "question": "Can I take your appointment reference or the patient's name?", "required": true },
                  { "id": "patientName",    "order": 2, "nextQuestionId": "newDateTime",  "question": "And the patient's full name?", "required": true },
                  { "id": "newDateTime",    "order": 3, "slotType": "datetime",           "question": "What date and time would you prefer instead?", "required": true }
                ]
              }
            },
            {
              "id": "cancel_appointment", "name": "Cancel Appointment", "type": "lookup",
              "triggers": ["cancel","cancel appointment","don't need","cancel my appointment"],
              "questionnaire": {
                "startQuestionId": "appointmentRef",
                "questions": [
                  { "id": "appointmentRef", "order": 1, "nextQuestionId": "patientName", "question": "Can I take your appointment reference or the patient's name?", "required": true },
                  { "id": "patientName",    "order": 2,                                  "question": "And the patient's name to confirm?", "required": true }
                ]
              }
            },
            {
              "id": "doctor_availability", "name": "Doctor Availability", "type": "lookup",
              "triggers": ["available","availability","when can i see","which doctor","today","is there anything","any slots","any availability"],
              "continueToIntentId": "book_appointment",
              "questionnaire": {
                "startQuestionId": "specialty",
                "questions": [
                  { "id": "specialty",     "order": 1, "nextQuestionId": "preferredDate", "question": "What type of appointment are you looking for? For example, GP, dermatology, or physiotherapy.", "required": true },
                  { "id": "preferredDate", "order": 2, "slotType": "date",                "question": "What date were you hoping for?", "required": true }
                ]
              }
            },
            {
              "id": "fee_location_inquiry", "name": "Fee or Location Inquiry", "type": "lookup",
              "triggers": ["how much","fee","cost","address","where","location","parking","opening hours","price"],
              "questionnaire": {
                "startQuestionId": "inquiryText",
                "questions": [
                  { "id": "inquiryText", "order": 1, "question": "Of course. What would you like to know? For example, our consultation fee, address, opening hours, or parking.", "required": true }
                ]
              }
            },
            {
              "id": "emergency_transfer", "name": "Emergency", "type": "transfer",
              "triggers": ["emergency","chest pain","can't breathe","urgent help","urgent","unconscious","severe"],
              "transferMessage": "This sounds urgent. Let me connect you to our emergency line immediately. Please stay on the line."
            }
          ]
        }
        """;

    private const string MedicareQuestionnaire = """
        {
          "openingScript": "Hi, this is Olivia calling from Demo Benefits Support. I'm reaching out to see if you'd like information about Medicare-related options that may be available to you. Do you have a few minutes?",
          "startQuestionId": "interestConfirmed",
          "questions": [
            {
              "id": "interestConfirmed", "order": 1, "question": "Great! Are you currently interested in learning about your Medicare options?",
              "required": true, "validValues": ["Yes","No"],
              "nextQuestionId": "leadName",
              "branches": [
                { "when": "No",  "action": "graceful_close" },
                { "when": "Yes", "nextQuestionId": "leadName" }
              ]
            },
            { "id": "leadName",       "order": 2, "nextQuestionId": "ageRange",       "question": "Can I get your full name?",                                                      "required": true  },
            {
              "id": "ageRange", "order": 3, "question": "Are you currently 65 or older, or approaching 65 soon?",
              "required": true, "validValues": ["65 or older","approaching 65","under 65"],
              "nextQuestionId": "currentCoverage",
              "branches": [
                { "when": "under 65", "action": "disqualify" },
                { "when": "*",        "nextQuestionId": "currentCoverage" }
              ]
            },
            { "id": "currentCoverage", "order": 4, "nextQuestionId": "state",         "question": "Do you currently have Medicare Part A or Part B, or any other health coverage?","required": true  },
            { "id": "state",           "order": 5, "nextQuestionId": "phone",          "question": "What state do you currently live in?",                                          "required": true  },
            { "id": "phone",           "order": 6, "nextQuestionId": "callbackTime",   "question": "What is the best phone number for a licensed specialist to reach you?",        "required": true  },
            { "id": "callbackTime",    "order": 7,                                     "question": "And what time works best for a callback — morning, afternoon, or evening?",    "required": true,  "validValues": ["Morning","Afternoon","Evening"] }
          ]
        }
        """;

    private const string AcaQuestionnaire = """
        {
          "openingScript": "Hi, this is Noah from Demo Health Plans. I'm reaching out because you may qualify for a health coverage plan under the Affordable Care Act with reduced premiums. Do you have a few minutes?",
          "startQuestionId": "interestConfirmed",
          "questions": [
            {
              "id": "interestConfirmed", "order": 1, "question": "Great! Are you open to hearing about your health coverage options?",
              "required": true, "validValues": ["Yes","No"],
              "nextQuestionId": "firstName",
              "branches": [
                { "when": "No",  "action": "graceful_close" },
                { "when": "Yes", "nextQuestionId": "firstName" }
              ]
            },
            { "id": "firstName",              "order": 2,  "nextQuestionId": "state",               "question": "Can I get your first name?",                                                             "required": true  },
            { "id": "state",                  "order": 3,  "nextQuestionId": "currentInsuranceStatus","question": "What state do you currently live in?",                                                  "required": true  },
            { "id": "currentInsuranceStatus", "order": 4,  "nextQuestionId": "householdSize",         "question": "Do you currently have health insurance?",                                              "required": true,  "validValues": ["Yes","No"] },
            { "id": "householdSize",          "order": 5,  "nextQuestionId": "incomeRange",           "question": "How many people are in your household, including yourself?",                           "required": true  },
            { "id": "incomeRange",            "order": 6,  "nextQuestionId": "coverageInterest",      "question": "Roughly what is your annual household income — for example, under $30,000, $30k to $60k, or above?", "required": false },
            {
              "id": "coverageInterest", "order": 7, "question": "Are you looking for individual or family coverage?",
              "required": true, "validValues": ["Individual","Family","None"],
              "nextQuestionId": "tobaccoUse",
              "branches": [
                { "when": "None", "action": "graceful_close" },
                { "when": "*",    "nextQuestionId": "tobaccoUse" }
              ]
            },
            { "id": "tobaccoUse",   "order": 8, "nextQuestionId": "phone",        "question": "Do you currently use tobacco products?",                                   "required": false, "validValues": ["Yes","No"] },
            { "id": "phone",        "order": 9, "nextQuestionId": "callbackTime", "question": "What is the best phone number for a licensed agent to reach you?",         "required": true  },
            { "id": "callbackTime", "order": 10,                                  "question": "And what time works best — morning, afternoon, or evening?",               "required": true,  "validValues": ["Morning","Afternoon","Evening"] }
          ]
        }
        """;

    private const string FeQuestionnaire = """
        {
          "openingScript": "Hi, this is Emma from Demo Life Plans. I'm calling about final expense life insurance — a whole-life policy with no medical exam required that helps cover funeral and end-of-life costs so your family is protected. Is this a good time to talk?",
          "startQuestionId": "interestConfirmed",
          "questions": [
            {
              "id": "interestConfirmed", "order": 1, "question": "Great! Are you open to hearing about coverage options?",
              "required": true, "validValues": ["Yes","No"],
              "nextQuestionId": "firstName",
              "branches": [
                { "when": "No",  "action": "graceful_close" },
                { "when": "Yes", "nextQuestionId": "firstName" }
              ]
            },
            { "id": "firstName", "order": 2, "nextQuestionId": "age",        "question": "Can I start with your first name?",                                                          "required": true },
            { "id": "age",       "order": 3, "nextQuestionId": "state",      "question": "And may I ask your age? Our plans are available for individuals between 50 and 85.",        "required": true },
            { "id": "state",     "order": 4, "nextQuestionId": "tobaccoUse", "question": "What state do you currently live in?",                                                       "required": true },
            {
              "id": "tobaccoUse", "order": 5, "question": "Do you currently smoke or use tobacco products?",
              "required": true, "validValues": ["Yes","No"],
              "nextQuestionId": "healthConditions_clean",
              "branches": [
                { "when": "Yes", "nextQuestionId": "healthConditions_tobacco" },
                { "when": "No",  "nextQuestionId": "healthConditions_clean"   }
              ]
            },
            {
              "id": "healthConditions_tobacco", "slotId": "healthConditions", "order": 6,
              "question": "Given that you use tobacco products, have you also been diagnosed with any serious health conditions such as cancer, heart disease, or kidney failure in the last two years?",
              "required": true, "validValues": ["Yes","No"],
              "nextQuestionId": "coverageAmount",
              "branches": [
                { "when": "Yes", "nextQuestionId": "coverageAmount", "setSlots": { "planType": "graded_benefit"  } },
                { "when": "No",  "nextQuestionId": "coverageAmount", "setSlots": { "planType": "graded_standard" } }
              ]
            },
            {
              "id": "healthConditions_clean", "slotId": "healthConditions", "order": 7,
              "question": "Have you been diagnosed with any serious health conditions such as cancer, heart disease, or kidney failure in the last two years?",
              "required": true, "validValues": ["Yes","No"],
              "nextQuestionId": "coverageAmount",
              "branches": [
                { "when": "Yes", "nextQuestionId": "coverageAmount", "setSlots": { "planType": "graded_benefit"   } },
                { "when": "No",  "nextQuestionId": "coverageAmount", "setSlots": { "planType": "simplified_issue" } }
              ]
            },
            { "id": "coverageAmount",  "order": 8, "nextQuestionId": "beneficiaryName", "question": "How much coverage are you looking for? We offer plans from $5,000 up to $25,000.", "required": true },
            { "id": "beneficiaryName", "order": 9, "nextQuestionId": "phone",            "question": "Who would you like listed as the beneficiary on the policy?",                     "required": true },
            { "id": "phone",           "order": 10,"nextQuestionId": "callbackTime",     "question": "What is the best phone number for a licensed agent to follow up with you?",       "required": true },
            { "id": "callbackTime",    "order": 11,                                      "question": "And what time works best for a callback — morning, afternoon, or evening?",       "required": true, "validValues": ["Morning","Afternoon","Evening"] }
          ]
        }
        """;

    // ── Seed records ─────────────────────────────────────────────────────────

    public static readonly IReadOnlyList<CampaignConfiguration> All =
    [
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000101"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.RestaurantClient, CampaignId = SeedIds.RestaurantCampaign,
            RequiredSlotsJson   = """["customerName","phone","fulfillmentType","items","paymentMethod"]""",
            AllowedToolsJson    = """["MenuCategorySearchTool","MenuItemSearchTool","DishInfoTool","ListDealsTool","CartUpdateTool","RestaurantTotalTool","SaveRestaurantOrderTool"]""",
            QuestionnaireJson   = RestaurantQuestionnaire,
            ValidationRulesJson = """{"deliveryFee":3.99,"taxRatePercent":0.0,"currency":"GBP","freeDeliveryThreshold":20.0}""",
            HumanTransferJson   = """{"enabled":false,"mode":"Disabled","fallbackWhenDisabled":"SaveAndClose"}""",
            RagSettingsJson     = """{"enabled":true,"topK":4,"minScore":0.72,"allowedDocumentTypes":["FAQ","Policy","ServiceInfo"]}"""
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000102"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CampaignId = SeedIds.CourierCampaign,
            RequiredSlotsJson = """["customerName","phone","pickupAddress","dropoffAddress","weightKg","packageType","urgency"]""",
            AllowedToolsJson  = """["GeocodeAddressTool","DistanceCalculatorTool","CourierQuoteTool","SaveCourierOrderTool"]""",
            QuestionnaireJson = CourierQuestionnaire,
            HumanTransferJson = """{"enabled":false,"mode":"Disabled","fallbackWhenDisabled":"SaveAndClose"}"""
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000103"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.CabClient, CampaignId = SeedIds.CabCampaign,
            RequiredSlotsJson   = """["customerName","phone","pickupLocation","dropoffLocation","pickupDateTime","passengerCount","vehicleType"]""",
            AllowedToolsJson    = """["CabFareEstimateTool","CabBookingTool"]""",
            QuestionnaireJson   = CabQuestionnaire,
            HumanTransferJson   = CabSeed.HumanTransferJson,
            ValidationRulesJson = $"{{\"fareSettings\":{CabSeed.FareSettingsJson},\"vehicleTypes\":{CabSeed.VehicleTypesJson}}}"
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000104"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.DoctorClient, CampaignId = SeedIds.DoctorCampaign,
            RequiredSlotsJson   = """["patientName","phone","reasonForVisit","preferredDateTime","preferredDoctor","branch"]""",
            AllowedToolsJson    = """["DoctorAvailabilityTool","DoctorAppointmentBookingTool","HumanHandoffTool"]""",
            QuestionnaireJson   = DoctorQuestionnaire,
            HumanTransferJson   = DoctorSeed.HumanTransferJson,
            ValidationRulesJson = DoctorSeed.DoctorDirectoryJson
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000105"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, CampaignId = SeedIds.MedicareCampaign,
            RequiredSlotsJson = """["leadName","phone","ageRange","currentCoverage","callbackTime"]""",
            AllowedToolsJson  = """["LeadQualificationTool","SalesScriptTool","ObjectionHandlingTool","HumanHandoffTool"]""",
            QuestionnaireJson = MedicareQuestionnaire,
            HumanTransferJson = """{"enabled":true,"mode":"OnlyOnUserRequest","transferNumber":"+441234567892"}"""
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000106"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, CampaignId = SeedIds.AcaCampaign,
            RequiredSlotsJson = """["firstName","phone","currentInsuranceStatus","householdSize","coverageInterest","callbackTime"]""",
            AllowedToolsJson  = """["LeadQualificationTool","SalesScriptTool","ObjectionHandlingTool","HumanHandoffTool"]""",
            QuestionnaireJson = AcaQuestionnaire
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000107"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, CampaignId = SeedIds.FeCampaign,
            RequiredSlotsJson = """["firstName","phone","age","tobaccoUse","healthConditions","coverageAmount","callbackTime"]""",
            AllowedToolsJson  = """["LeadQualificationTool","SalesScriptTool","ObjectionHandlingTool","HumanHandoffTool"]""",
            QuestionnaireJson = FeQuestionnaire
        }
    ];
}
