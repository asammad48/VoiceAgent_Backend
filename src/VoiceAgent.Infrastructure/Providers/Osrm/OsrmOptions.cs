namespace VoiceAgent.Infrastructure.Providers.Osrm;

public class OsrmOptions
{
    public const string SectionName = "Osrm";
    public string BaseUrl { get; set; } = "https://router.project-osrm.org";
}
