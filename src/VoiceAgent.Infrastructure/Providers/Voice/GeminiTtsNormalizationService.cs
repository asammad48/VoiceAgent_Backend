using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceAgent.Application.Interfaces.Voice;
using VoiceAgent.Application.Services.Voice;
using VoiceAgent.Infrastructure.Providers.Llm;

namespace VoiceAgent.Infrastructure.Providers.Voice;

/// <summary>
/// Uses Gemini to convert assistant reply text into natural spoken English before ElevenLabs.
/// Reads TtsLocale from IOptions so the prompt is locale-aware (clock format, date order, currency).
/// </summary>
public sealed class GeminiTtsNormalizationService(
    GeminiClient geminiClient,
    IOptions<GeminiOptions> geminiOptions,
    IOptions<TtsLocale> localeOptions,
    ILogger<GeminiTtsNormalizationService> logger) : ITtsNormalizationService
{
    private readonly TtsLocale _locale = localeOptions.Value;

    public async Task<string> NormalizeForTtsAsync(string text, CancellationToken ct = default)
    {
        if (geminiOptions.Value.UseMockProviders) return text;

        logger.LogInformation("[TtsNorm] Normalizing {Chars} chars for TTS", text.Length);

        var clockHint = _locale.Use24HourClock
            ? "Use 24-hour clock: \"14:30\" → \"fourteen thirty\"."
            : "Use 12-hour clock: \"14:30\" → \"two thirty PM\", \"09:05\" → \"nine oh five AM\".";

        var dateHint = _locale.DateOrder == "mdy"
            ? "Dates are MM/DD/YYYY: \"05/18/2026\" → \"May eighteenth twenty twenty-six\"."
            : "Dates are DD/MM/YYYY: \"18/05/2026\" → \"the eighteenth of May twenty twenty-six\".";

        var prompt = $"""
            You are a text-to-speech preprocessor for a voice AI agent. Convert the input text into clear, natural spoken English that will sound correct when read aloud. Return ONLY the converted text — no preamble, no quotation marks, no explanation.

            Locale rules:
            - {clockHint}
            - {dateHint}

            Conversion rules (apply all that are relevant):
            1. Numbers → words: "45" → "forty-five", "1,500" → "one thousand five hundred", "2.5" → "two point five".
            2. Currency: "£45.50" → "forty-five pounds fifty pence", "£45.00" → "forty-five pounds", "Rs.500" → "five hundred rupees", "PKR 1,200" → "one thousand two hundred Pakistani rupees", "$12.99" → "twelve dollars ninety-nine cents", "€20" → "twenty euros". Currency ranges like "Rs.500-700" → "five hundred to seven hundred rupees".
            3. Phone numbers → digit-by-digit with grouping pauses: "03005516522" → "zero three zero zero, five five one, six five two two". Strip parentheses, dashes, spaces first. International prefix "+" → "plus".
            4. Units: "5.2 km" → "five point two kilometers", "2.5 kg" → "two point five kilograms", "5 mins" → "five minutes", "2 hrs" → "two hours", "30 secs" → "thirty seconds", "25°C" → "twenty-five degrees Celsius", "70°F" → "seventy degrees Fahrenheit", "sq ft" → "square feet", "cm" → "centimeters", "mm" → "millimeters", "ml" → "milliliters".
            5. Abbreviations: "ETA" → "estimated time of arrival", "TBC" → "to be confirmed", "TBD" → "to be determined", "ASAP" → "as soon as possible", "N/A" → "not applicable".
            6. Address abbreviations: "Rd." → "Road", "Ave." → "Avenue", "Blvd." → "Boulevard", "Ln." → "Lane", "Sq." → "Square". Only expand "St." to "Street" when it follows another word (e.g. "Main St." → "Main Street"). Do not expand "Dr." unless it clearly means "Drive" in an address context.
            7. Ordinals: "1st" → "first", "2nd" → "second", "3rd" → "third", "22nd" → "twenty-second".
            8. Symbols: "&" → "and", "#12" → "number twelve", "@" in email → "at", "/" between units → "per" (e.g. "km/h" → "kilometers per hour"), ">" → "greater than", "<" → "less than".
            9. Short alphanumeric IDs (mixed letters and digits, 6-10 chars, e.g. "A3F7B2C9") → spell each character with spaces: "A 3 F 7 B 2 C 9". Hyphenated IDs like "ORD-12345" → "O R D, one two three four five".
            10. Percentages: "10%" → "ten percent", "99.9%" → "ninety-nine point nine percent".
            11. Fractions: "1/2" → "one half", "3/4" → "three quarters", "24/7" → "twenty-four seven".
            12. Day ranges: "Mon-Fri" → "Monday to Friday", "9am-5pm" → "nine AM to five PM".
            13. Postal codes (UK style): "BD7 1DP" → "B D 7, 1 D P".
            14. URLs/websites: shorten to domain only, spoken naturally: "www.example.com" → "example dot com". Emails: "user@example.com" → "user at example dot com".
            15. Keep text conversational and concise. Do not add words that were not in the original. Do not remove meaning.

            Text to convert:
            {text}
            """;

        var raw = await geminiClient.GenerateAsync(prompt, ct);
        var normalized = raw.Trim().Trim('"', '\'');

        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.Equals("[mock-gemini]", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("[TtsNorm] LLM returned empty/mock — using original text");
            return text;
        }

        logger.LogInformation("[TtsNorm] Normalized {In} → {Out} chars", text.Length, normalized.Length);
        return normalized;
    }
}
