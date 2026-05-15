namespace VoiceAgent.Application.Interfaces;

public interface ILocationNormalizationService
{
    /// <summary>
    /// Converts messy spoken location descriptions into clean geocoding candidates
    /// in a single LLM call. Nominatim receives the LLM output, never raw speech.
    /// Falls back to original strings if the LLM call fails.
    /// </summary>
    Task<(string Pickup, string Dropoff)> NormalizeLocationsAsync(
        string pickup, string dropoff,
        CancellationToken ct = default);
}
