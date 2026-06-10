using System.Text.Json;

namespace UnicodeCldr.LocaleData.Generator;

/// <summary>One CLDR relative-time pattern for a (locale, unit, style, tense, count).</summary>
/// <param name="Locale">The CLDR locale id (lowercased).</param>
/// <param name="Unit">The ECMA-402 relative-time unit (e.g. <c>"day"</c>, <c>"year"</c>).</param>
/// <param name="Style">The width: <c>"long"</c>, <c>"short"</c> or <c>"narrow"</c>.</param>
/// <param name="Tense"><c>"future"</c> or <c>"past"</c>.</param>
/// <param name="Count">The plural category (e.g. <c>"one"</c>, <c>"other"</c>).</param>
/// <param name="Pattern">The pattern with a <c>{0}</c> placeholder (e.g. <c>"in {0} days"</c>).</param>
public sealed record RelativeTimePattern(string Locale, string Unit, string Style, string Tense, string Count, string Pattern);

/// <summary>One CLDR exact relative phrase for a (locale, unit, style, offset).</summary>
/// <param name="Locale">The CLDR locale id (lowercased).</param>
/// <param name="Unit">The ECMA-402 relative-time unit (e.g. <c>"day"</c>).</param>
/// <param name="Style">The width: <c>"long"</c>, <c>"short"</c> or <c>"narrow"</c>.</param>
/// <param name="Offset">The signed numeric offset (e.g. <c>-1</c> for "yesterday").</param>
/// <param name="Phrase">The exact phrase (e.g. <c>"yesterday"</c>).</param>
public sealed record RelativeTimeExact(string Locale, string Unit, string Style, int Offset, string Phrase);

/// <summary>The combined result of parsing all locales' <c>dateFields.json</c>.</summary>
public sealed record RelativeTimeData(IReadOnlyList<RelativeTimePattern> Patterns, IReadOnlyList<RelativeTimeExact> Exacts);

/// <summary>
/// Parses CLDR <c>dateFields.json</c> into the relative-time numeric patterns and
/// exact phrases for <c>Intl.RelativeTimeFormat</c>, limited to the ECMA-402
/// sanctioned units (second…year). The "long" style is the bare field (e.g.
/// <c>"day"</c>); "short" and "narrow" are the <c>-short</c>/<c>-narrow</c> suffixed
/// fields.
/// </summary>
public static class CldrRelativeTimeParser
{
    /// <summary>
    /// The ECMA-402 relative-time units, ordered by magnitude. This is also the
    /// emit order used by <see cref="CldrRelativeTimeCodeGenerator"/>.
    /// </summary>
    public static readonly string[] Units =
    {
        "second", "minute", "hour", "day", "week", "month", "quarter", "year",
    };

    /// <summary>The ECMA-402 styles, paired with their CLDR field-name suffix.</summary>
    private static readonly (string Style, string Suffix)[] Styles =
    {
        ("long", ""),
        ("short", "-short"),
        ("narrow", "-narrow"),
    };

    private static readonly (string Tense, string Key)[] Tenses =
    {
        ("future", "relativeTime-type-future"),
        ("past", "relativeTime-type-past"),
    };

    private const string CountPrefix = "relativeTimePattern-count-";
    private const string ExactPrefix = "relative-type-";

    public static RelativeTimeData ParseAll(string dataDirectory)
    {
        var patterns = new List<RelativeTimePattern>();
        var exacts = new List<RelativeTimeExact>();

        foreach (var dir in Directory.EnumerateDirectories(dataDirectory).OrderBy(static d => d, StringComparer.Ordinal))
        {
            var file = Path.Combine(dir, "dateFields.json");
            if (File.Exists(file))
            {
                ParseFile(file, Path.GetFileName(dir).ToLowerInvariant(), patterns, exacts);
            }
        }

        return new RelativeTimeData(patterns, exacts);
    }

    private static void ParseFile(string path, string locale, List<RelativeTimePattern> patterns, List<RelativeTimeExact> exacts)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("main", out var main))
        {
            return;
        }

        using var localeEnumerator = main.EnumerateObject().GetEnumerator();
        if (!localeEnumerator.MoveNext()
            || !localeEnumerator.Current.Value.TryGetProperty("dates", out var dates)
            || !dates.TryGetProperty("fields", out var fields))
        {
            return;
        }

        foreach (var unit in Units)
        {
            foreach (var (style, suffix) in Styles)
            {
                if (!fields.TryGetProperty(unit + suffix, out var field))
                {
                    continue;
                }

                foreach (var (tense, key) in Tenses)
                {
                    if (!field.TryGetProperty(key, out var section))
                    {
                        continue;
                    }

                    foreach (var property in section.EnumerateObject())
                    {
                        if (!property.Name.StartsWith(CountPrefix, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var count = property.Name[CountPrefix.Length..];
                        patterns.Add(new RelativeTimePattern(locale, unit, style, tense, count, property.Value.GetString()!));
                    }
                }

                foreach (var property in field.EnumerateObject())
                {
                    if (!property.Name.StartsWith(ExactPrefix, StringComparison.Ordinal)
                        || !int.TryParse(property.Name[ExactPrefix.Length..], out var offset))
                    {
                        continue;
                    }

                    exacts.Add(new RelativeTimeExact(locale, unit, style, offset, property.Value.GetString()!));
                }
            }
        }
    }
}
