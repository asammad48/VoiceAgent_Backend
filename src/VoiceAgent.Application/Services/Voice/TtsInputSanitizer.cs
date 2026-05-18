using System.Text.RegularExpressions;

namespace VoiceAgent.Application.Services.Voice;

/// <summary>
/// Strips non-speakable content from text before TTS normalization:
/// debug speaker labels, markdown formatting, JSON/tool artefacts,
/// URLs, emails, emojis, and duplicate whitespace.
/// Pure static — no dependencies.
/// </summary>
public static class TtsInputSanitizer
{
    // "User: ..." / "Assistant: ..." labels at line start
    private static readonly Regex DebugLabel = new(
        @"^(User|Assistant|System|ToolResult|Tool|Bot|Human|AI)\s*:\s*",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    // Markdown bold (**text** or __text__)
    private static readonly Regex Bold = new(
        @"\*{2}([^*\n]+)\*{2}|_{2}([^_\n]+)_{2}",
        RegexOptions.Compiled);

    // Markdown italic (*text* or _text_) — negative lookahead prevents touching bold
    private static readonly Regex Italic = new(
        @"(?<!\*)\*(?!\*)([^*\n]+)(?<!\*)\*(?!\*)|(?<!_)_(?!_)([^_\n]+)(?<!_)_(?!_)",
        RegexOptions.Compiled);

    // Inline code backticks
    private static readonly Regex InlineCode = new(@"`[^`\n]+`", RegexOptions.Compiled);

    // Markdown headers (## Heading)
    private static readonly Regex Header = new(@"^\s*#{1,6}\s+", RegexOptions.Multiline | RegexOptions.Compiled);

    // Bullet / list markers at line start
    private static readonly Regex Bullet = new(@"^\s*[-*•]\s+", RegexOptions.Multiline | RegexOptions.Compiled);

    // JSON-like key-value fragments: {"key":"value"} or ["item"]
    // Conservative pattern — requires quotes around key+value to avoid matching address ranges
    private static readonly Regex JsonFragment = new(
        @"\{(?:[^{}]|""[^""]*""\s*:\s*""[^""]*""\s*(?:,\s*""[^""]*""\s*:\s*""[^""]*""\s*)*)\}",
        RegexOptions.Compiled);

    // Full URLs
    private static readonly Regex Url = new(
        @"https?://\S+|www\.\S+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Email addresses
    private static readonly Regex Email = new(
        @"\b[\w.+\-]+@[\w.\-]+\.[a-zA-Z]{2,}\b",
        RegexOptions.Compiled);

    // Common Unicode emoji blocks (covers most standard emoji)
    private static readonly Regex Emoji = new(
        @"[☀-⛿✀-➿]|[\uD83C-\uDBFF][\uDC00-\uDFFF]",
        RegexOptions.Compiled);

    // Repeated punctuation (e.g. "..." or "!!")
    private static readonly Regex RepeatedPunct = new(@"([.!?]){2,}", RegexOptions.Compiled);

    public static string Sanitize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        text = DebugLabel.Replace(text, "");
        text = JsonFragment.Replace(text, "");
        text = Bold.Replace(text,   m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
        text = Italic.Replace(text, m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
        text = InlineCode.Replace(text, "");
        text = Header.Replace(text, "");
        text = Bullet.Replace(text, "");
        text = Emoji.Replace(text, "");
        text = Url.Replace(text,   m => SpeakableUrl(m.Value));
        text = Email.Replace(text, m => SpeakableEmail(m.Value));

        // Line breaks → natural sentence pause
        text = Regex.Replace(text, @"\r?\n+", ". ");
        // Collapse extra horizontal whitespace
        text = Regex.Replace(text, @"[ \t]{2,}", " ");
        // Reduce repeated punctuation
        text = RepeatedPunct.Replace(text, "$1");

        return text.Trim();
    }

    // https://www.example.com/path → "example dot com"
    private static string SpeakableUrl(string url)
    {
        url = Regex.Replace(url, @"^https?://", "", RegexOptions.IgnoreCase);
        url = Regex.Replace(url, @"^www\.", "",   RegexOptions.IgnoreCase);
        // Take only host, split into parts at dots
        var host = url.Split('/', '?', '#')[0];
        return host.Replace(".", " dot ");
    }

    // user@example.com → "user at example dot com"
    private static string SpeakableEmail(string email)
    {
        var at     = email.IndexOf('@');
        var local  = email[..at];
        var domain = email[(at + 1)..].Replace(".", " dot ");
        return $"{local} at {domain}";
    }
}
