using System.Text.Json;
using Microsoft.Extensions.Options;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Infrastructure.Providers.Llm;

namespace VoiceAgent.Infrastructure.Providers;

/// <summary>
/// Converts messy, spoken location descriptions into clean geocoding candidates
/// that Nominatim can search. Acts as a pre-processing step between raw voice
/// input and the geocoding API — Nominatim never receives raw speech.
/// </summary>
public sealed class GeminiLocationNormalizationService(
    GeminiClient geminiClient,
    IOptions<GeminiOptions> options) : ILocationNormalizationService
{
    public async Task<(string Pickup, string Dropoff)> NormalizeLocationsAsync(
        string pickup, string dropoff,
        CancellationToken ct = default)
    {
        if (options.Value.UseMockProviders)
            return (pickup, dropoff);

        // $$""" prefix: single { is a literal brace; {{expr}} is interpolation
        var prompt = $$"""
            You are a speech-to-geocoding converter for a voice booking agent.
            A caller has spoken two location names in natural, potentially messy speech.
            Your job is to convert each into a clean, structured string that the Nominatim geocoding API can search successfully.

            Caller said for pickup : {{pickup}}
            Caller said for dropoff: {{dropoff}}

            Output rules:
            - Return ONLY valid JSON on a single line — no markdown, no explanation.
            - Format: {"pickup":"<candidate>","dropoff":"<candidate>"}
            - Each candidate must be a Nominatim-searchable string, e.g.:
                "Heathrow Airport, London, UK"
                "Tesco, Oxford Street, London, UK"
                "10 Downing Street, Westminster, London, UK"
                "Manchester Piccadilly Station, Manchester, UK"
            - Strip filler words ("you know", "the one near", "I usually go to", etc.)
            - Expand abbreviations where obvious (e.g. "Manc" → "Manchester")
            - If a postcode or zip code is spoken, include it verbatim
            - If the caller named a well-known landmark or chain, use its official name
            - If a location is already a clean address, return it unchanged
            - Never invent an address — if genuinely unresolvable, return the original spoken text
            """;

        try
        {
            var raw = await geminiClient.GenerateAsync(prompt, ct);
            var json = raw.Trim();

            // Strip markdown fences if the model wrapped the JSON
            if (json.StartsWith("```", StringComparison.Ordinal))
                json = string.Join('\n', json.Split('\n').Where(l => !l.StartsWith("```"))).Trim();

            using var doc = JsonDocument.Parse(json);
            var root        = doc.RootElement;
            var pickupNorm  = root.GetProperty("pickup").GetString();
            var dropoffNorm = root.GetProperty("dropoff").GetString();

            return (
                string.IsNullOrWhiteSpace(pickupNorm)  ? pickup  : pickupNorm,
                string.IsNullOrWhiteSpace(dropoffNorm) ? dropoff : dropoffNorm
            );
        }
        catch
        {
            // Fall back to original strings — the caller's best effort is better than nothing
            return (pickup, dropoff);
        }
    }
}
