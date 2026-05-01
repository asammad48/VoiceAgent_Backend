namespace VoiceAgent.Application.Interfaces.Providers;
public interface IGeocodingProvider
{
    Task<(double Latitude, double Longitude)?> GeocodeAsync(string address, CancellationToken ct = default);
}
