namespace VoiceAgent.Infrastructure.Providers.Nominatim;

public class NominatimOptions
{
    public const string SectionName = "Nominatim";
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";
}
