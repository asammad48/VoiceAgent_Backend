using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class CampaignConfigurationSeed
{
    // ── Questionnaire JSON constants ──────────────────────────────────────────

    private const string RestaurantQuestionnaire = """
        {
          "openingScript": "Hi! Welcome to our restaurant. I'm Maya, your ordering assistant. What can I get for you today?",
          "questions": [
            { "id": "items",           "order": 1, "question": "What would you like to order? I can tell you about our menu categories or deals.", "required": true,  "validValues": null },
            { "id": "fulfillmentType", "order": 2, "question": "Would you like delivery or pickup?",                                                "required": true,  "validValues": ["delivery","pickup"] },
            { "id": "paymentMethod",   "order": 3, "question": "How would you like to pay — cash or card?",                                         "required": true,  "validValues": ["cash","card"] },
            { "id": "customerName",    "order": 4, "question": "Can I take your name for the order?",                                              "required": true,  "validValues": null },
            { "id": "phone",           "order": 5, "question": "And your phone number in case we need to reach you?",                              "required": true,  "validValues": null }
          ]
        }
        """;

    private const string CourierQuestionnaire = """
        {
          "openingScript": "Hi! This is Sam from our courier service. I can help you get a delivery quote and book a pickup. Where are we collecting from today?",
          "questions": [
            { "id": "pickupAddress",  "order": 1, "question": "What is the pickup address?",                              "required": true,  "validValues": null },
            { "id": "dropoffAddress", "order": 2, "question": "And where should we deliver it?",                          "required": true,  "validValues": null },
            { "id": "weightKg",       "order": 3, "question": "What is the approximate weight of the package in kilograms?","required": true,  "validValues": null },
            { "id": "packageType",    "order": 4, "question": "Is it a standard parcel, document, or fragile item?",     "required": true,  "validValues": ["standard","document","fragile"] },
            { "id": "urgency",        "order": 5, "question": "Do you need standard delivery or same-day?",              "required": true,  "validValues": ["standard","same_day"] },
            { "id": "customerName",   "order": 6, "question": "Can I take your name for the booking?",                   "required": true,  "validValues": null },
            { "id": "phone",          "order": 7, "question": "And your contact number?",                                "required": true,  "validValues": null }
          ]
        }
        """;

    private const string CabQuestionnaire = """
        {
          "openingScript": "Hi! I'm Adam, your cab booking assistant. I'll get you a quick quote and confirm your ride. Where are we picking you up from?",
          "questions": [
            { "id": "pickupLocation",  "order": 1, "question": "Where should we pick you up?",                            "required": true,  "validValues": null },
            { "id": "dropoffLocation", "order": 2, "question": "And where are you heading?",                              "required": true,  "validValues": null },
            { "id": "pickupDateTime",  "order": 3, "question": "What date and time do you need the cab?",                 "required": true,  "validValues": null },
            { "id": "passengerCount",  "order": 4, "question": "How many passengers will be travelling?",                 "required": true,  "validValues": null },
            { "id": "vehicleType",     "order": 5, "question": "What type of vehicle do you prefer — Standard, Executive, 6-Seater, or Wheelchair Accessible?", "required": true, "validValues": ["standard","executive","6-seater","wheelchair accessible"] },
            { "id": "customerName",    "order": 6, "question": "Can I take your name for the booking?",                  "required": true,  "validValues": null },
            { "id": "phone",           "order": 7, "question": "And your phone number?",                                 "required": true,  "validValues": null }
          ]
        }
        """;

    private const string DoctorQuestionnaire = """
        {
          "openingScript": "Hi, this is Sara from City Health Clinic. I can help you request an appointment. What is the appointment for?",
          "questions": [
            { "id": "reasonForVisit",   "order": 1, "question": "What is the reason for your visit?",                              "required": true,  "validValues": null },
            { "id": "patientName",      "order": 2, "question": "Can I take the patient's full name?",                             "required": true,  "validValues": null },
            { "id": "phone",            "order": 3, "question": "What is the best contact number for the patient?",                "required": true,  "validValues": null },
            { "id": "preferredDateTime","order": 4, "question": "What day and time would you prefer for the appointment?",         "required": true,  "validValues": null },
            { "id": "preferredDoctor",  "order": 5, "question": "Do you have a preferred doctor, or is any doctor fine?",         "required": false, "validValues": null },
            { "id": "branch",           "order": 6, "question": "Which of our clinic locations is most convenient for you?",       "required": false, "validValues": null }
          ]
        }
        """;

    private const string MedicareQuestionnaire = """
        {
          "openingScript": "Hi, this is Olivia calling from Demo Benefits Support. I'm reaching out to see if you'd like information about Medicare-related options that may be available to you. Do you have a few minutes?",
          "questions": [
            { "id": "interestConfirmed", "order": 1, "question": "Great! Are you currently interested in learning about your Medicare options?",         "required": true,  "validValues": ["yes","no"] },
            { "id": "leadName",          "order": 2, "question": "Can I get your full name?",                                                           "required": true,  "validValues": null },
            { "id": "ageRange",          "order": 3, "question": "Are you currently 65 or older, or approaching 65 soon?",                              "required": true,  "validValues": ["65 or older","approaching 65","under 65"] },
            { "id": "currentCoverage",   "order": 4, "question": "Do you currently have Medicare Part A or Part B, or any other health coverage?",      "required": true,  "validValues": null },
            { "id": "state",             "order": 5, "question": "What state do you currently live in?",                                               "required": true,  "validValues": null },
            { "id": "phone",             "order": 6, "question": "What is the best phone number for a licensed specialist to reach you?",              "required": true,  "validValues": null },
            { "id": "callbackTime",      "order": 7, "question": "And what time works best for a callback — morning, afternoon, or evening?",          "required": true,  "validValues": ["morning","afternoon","evening"] }
          ]
        }
        """;

    private const string AcaQuestionnaire = """
        {
          "openingScript": "Hi, this is Noah from Demo Health Plans. I'm reaching out because you may qualify for a health coverage plan under the Affordable Care Act with reduced premiums. Do you have a few minutes?",
          "questions": [
            { "id": "interestConfirmed",     "order": 1, "question": "Great! Are you open to hearing about your health coverage options?",                                  "required": true,  "validValues": ["yes","no"] },
            { "id": "firstName",             "order": 2, "question": "Can I get your first name?",                                                                         "required": true,  "validValues": null },
            { "id": "state",                 "order": 3, "question": "What state do you currently live in?",                                                               "required": true,  "validValues": null },
            { "id": "currentInsuranceStatus","order": 4, "question": "Do you currently have health insurance?",                                                            "required": true,  "validValues": ["yes","no"] },
            { "id": "householdSize",         "order": 5, "question": "How many people are in your household, including yourself?",                                         "required": true,  "validValues": null },
            { "id": "incomeRange",           "order": 6, "question": "Roughly what is your annual household income — for example, under $30,000, $30k to $60k, or above?","required": false, "validValues": null },
            { "id": "coverageInterest",      "order": 7, "question": "Are you looking for individual or family coverage?",                                                "required": true,  "validValues": ["individual","family"] },
            { "id": "tobaccoUse",            "order": 8, "question": "Do you currently use tobacco products?",                                                            "required": false, "validValues": ["yes","no"] },
            { "id": "phone",                 "order": 9, "question": "What is the best phone number for a licensed agent to reach you?",                                  "required": true,  "validValues": null },
            { "id": "callbackTime",          "order": 10,"question": "And what time works best — morning, afternoon, or evening?",                                        "required": true,  "validValues": ["morning","afternoon","evening"] }
          ]
        }
        """;

    private const string FeQuestionnaire = """
        {
          "openingScript": "Hi, this is Emma from Demo Life Plans. I'm calling about final expense life insurance — a whole-life policy with no medical exam required that helps cover funeral and end-of-life costs so your family is protected. Is this a good time to talk?",
          "questions": [
            { "id": "interestConfirmed", "order": 1, "question": "Great! Are you open to hearing about coverage options?",                                                            "required": true,  "validValues": ["yes","no"] },
            { "id": "firstName",         "order": 2, "question": "Can I start with your first name?",                                                                                "required": true,  "validValues": null },
            { "id": "age",               "order": 3, "question": "And may I ask your age? Our plans are available for individuals between 50 and 85.",                               "required": true,  "validValues": null },
            { "id": "state",             "order": 4, "question": "What state do you currently live in?",                                                                             "required": true,  "validValues": null },
            { "id": "tobaccoUse",        "order": 5, "question": "Do you currently smoke or use tobacco products?",                                                                 "required": true,  "validValues": ["yes","no"] },
            { "id": "healthConditions",  "order": 6, "question": "Have you been diagnosed with any serious health conditions such as cancer, heart disease, or kidney failure in the last two years?", "required": true, "validValues": ["yes","no"] },
            { "id": "coverageAmount",    "order": 7, "question": "How much coverage are you looking for? We offer plans from $5,000 up to $25,000.",                                "required": true,  "validValues": null },
            { "id": "beneficiaryName",   "order": 8, "question": "Who would you like listed as the beneficiary on the policy?",                                                     "required": true,  "validValues": null },
            { "id": "phone",             "order": 9, "question": "What is the best phone number for a licensed agent to follow up with you?",                                       "required": true,  "validValues": null },
            { "id": "callbackTime",      "order": 10,"question": "And what time works best for a callback — morning, afternoon, or evening?",                                       "required": true,  "validValues": ["morning","afternoon","evening"] }
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
            RequiredSlotsJson    = """["customerName","phone","fulfillmentType","items","paymentMethod"]""",
            AllowedToolsJson     = """["MenuCategorySearchTool","MenuItemSearchTool","DishInfoTool","ListDealsTool","CartUpdateTool","RestaurantTotalTool","SaveRestaurantOrderTool"]""",
            QuestionnaireJson    = RestaurantQuestionnaire,
            HumanTransferJson    = """{"enabled":false,"mode":"Disabled","fallbackWhenDisabled":"SaveAndClose"}""",
            RagSettingsJson      = """{"enabled":true,"topK":4,"minScore":0.72,"allowedDocumentTypes":["FAQ","Policy","ServiceInfo"]}"""
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000102"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.CourierClient, CampaignId = SeedIds.CourierCampaign,
            RequiredSlotsJson    = """["customerName","phone","pickupAddress","dropoffAddress","weightKg","packageType","urgency"]""",
            AllowedToolsJson     = """["GeocodeAddressTool","DistanceCalculatorTool","CourierQuoteTool","SaveCourierOrderTool"]""",
            QuestionnaireJson    = CourierQuestionnaire,
            HumanTransferJson    = """{"enabled":false,"mode":"Disabled","fallbackWhenDisabled":"SaveAndClose"}"""
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000103"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.CabClient, CampaignId = SeedIds.CabCampaign,
            RequiredSlotsJson    = """["customerName","phone","pickupLocation","dropoffLocation","pickupDateTime","passengerCount","vehicleType"]""",
            AllowedToolsJson     = """["CabFareEstimateTool","CabBookingTool"]""",
            QuestionnaireJson    = CabQuestionnaire,
            HumanTransferJson    = CabSeed.HumanTransferJson,
            ValidationRulesJson  = $"{{\"fareSettings\":{CabSeed.FareSettingsJson},\"vehicleTypes\":{CabSeed.VehicleTypesJson}}}"
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000104"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.DoctorClient, CampaignId = SeedIds.DoctorCampaign,
            RequiredSlotsJson    = """["patientName","phone","reasonForVisit","preferredDateTime","preferredDoctor","branch"]""",
            AllowedToolsJson     = """["DoctorAvailabilityTool","DoctorAppointmentBookingTool","HumanHandoffTool"]""",
            QuestionnaireJson    = DoctorQuestionnaire,
            HumanTransferJson    = DoctorSeed.HumanTransferJson,
            ValidationRulesJson  = DoctorSeed.DoctorDirectoryJson
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000105"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.MedicareClient, CampaignId = SeedIds.MedicareCampaign,
            RequiredSlotsJson    = """["leadName","phone","ageRange","currentCoverage","interestLevel"]""",
            AllowedToolsJson     = """["LeadQualificationTool","SalesScriptTool","ObjectionHandlingTool","HumanHandoffTool"]""",
            QuestionnaireJson    = MedicareQuestionnaire,
            HumanTransferJson    = """{"enabled":true,"mode":"OnlyOnUserRequest","transferNumber":"+441234567892"}"""
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000106"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.AcaClient, CampaignId = SeedIds.AcaCampaign,
            RequiredSlotsJson    = """["firstName","phone","currentInsuranceStatus","householdSize","coverageInterest","callbackTime"]""",
            AllowedToolsJson     = """["LeadQualificationTool","SalesScriptTool","ObjectionHandlingTool","HumanHandoffTool"]""",
            QuestionnaireJson    = AcaQuestionnaire
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000107"),
            TenantId = SeedIds.Tenant, ClientId = SeedIds.FeClient, CampaignId = SeedIds.FeCampaign,
            RequiredSlotsJson    = """["firstName","phone","age","tobaccoUse","healthConditions","coverageAmount","callbackTime"]""",
            AllowedToolsJson     = """["LeadQualificationTool","SalesScriptTool","ObjectionHandlingTool","HumanHandoffTool"]""",
            QuestionnaireJson    = FeQuestionnaire
        }
    ];
}
