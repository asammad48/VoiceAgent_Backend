namespace VoiceAgent.Infrastructure.Persistence.Seed;

public static class DoctorSeed
{
    public const string HumanTransferJson =
        "{\"enabled\":true,\"mode\":\"OnlyOnHighRisk\",\"transferNumber\":\"+441234567891\",\"highRiskKeywords\":[\"chest pain\",\"cannot breathe\",\"severe bleeding\",\"unconscious\",\"suicidal\"]}";

    public const string DoctorDirectoryJson =
        "{\"doctors\":[{\"name\":\"Dr Ahmed Khan\",\"speciality\":\"General Practitioner\",\"availableDays\":[\"Monday\",\"Wednesday\",\"Friday\"]},{\"name\":\"Dr Sarah Wilson\",\"speciality\":\"Family Medicine\",\"availableDays\":[\"Tuesday\",\"Thursday\"]},{\"name\":\"Dr Emily Carter\",\"speciality\":\"Physiotherapy\",\"availableDays\":[\"Monday\",\"Thursday\"]}],\"appointmentTypes\":[\"General consultation\",\"Follow-up visit\",\"Physiotherapy\",\"Blood pressure check\",\"Minor illness consultation\"]}";
}
