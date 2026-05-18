namespace VoiceAgent.Application.Services.Voice;

/// <summary>
/// Locale settings that drive pronunciation choices in the TTS normalizer.
/// Bind via IOptions&lt;TtsLocale&gt; from the "TtsLocale" appsettings section.
/// </summary>
public sealed class TtsLocale
{
    public static readonly TtsLocale Default = new();

    /// <summary>false = 12-hour clock ("two thirty PM"), true = 24-hour ("fourteen thirty").</summary>
    public bool Use24HourClock { get; init; } = false;

    /// <summary>"dmy" = DD/MM/YYYY (UK/PK default), "mdy" = MM/DD/YYYY (US).</summary>
    public string DateOrder { get; init; } = "dmy";

    /// <summary>Prefer metric units when ambiguous (e.g. bare "m" → meters vs miles).</summary>
    public bool PreferMetric { get; init; } = true;
}
