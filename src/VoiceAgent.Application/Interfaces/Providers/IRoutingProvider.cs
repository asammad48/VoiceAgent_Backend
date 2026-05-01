namespace VoiceAgent.Application.Interfaces.Providers;
public interface IRoutingProvider
{
    Task<decimal?> GetDistanceKmAsync((double Latitude, double Longitude) from, (double Latitude, double Longitude) to, CancellationToken ct = default);
}
