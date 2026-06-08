using System.Text.Json;

namespace UnicodeCldr.LocaleData.Generator;

/// <summary>The four CLDR list-assembly patterns for one (locale, type, style).</summary>
/// <param name="Start">Pattern combining the first element with the rest.</param>
/// <param name="Middle">Pattern combining interior elements.</param>
/// <param name="End">Pattern combining the penultimate element with the last.</param>
/// <param name="Pair">Pattern for a two-element list.</param>
public sealed record CldrListPattern(string Start, string Middle, string End, string Pair);

/// <summary>All list patterns for one locale, keyed by an ECMA-402 "type|style" string.</summary>
/// <param name="Language">The CLDR locale id (typically a language subtag such as <c>"en"</c>).</param>
/// <param name="ByTypeStyle">Map from <c>"&lt;type&gt;|&lt;style&gt;"</c> (ECMA-402 names) to the pattern set.</param>
public sealed record LocaleListPatterns(string Language, IReadOnlyDictionary<string, CldrListPattern> ByTypeStyle);

/// <summary>
/// Parses CLDR <c>listPatterns.json</c> files (as published by the <c>cldr-json</c>
/// project) into the ECMA-402 ListFormat shape (type ∈ conjunction/disjunction/unit,
/// style ∈ long/short/narrow).
/// </summary>
public static class CldrListPatternsParser
{
    // ECMA-402 ListFormat type -> CLDR "listPattern-type-<x>" infix.
    private static readonly (string EcmaType, string CldrType)[] Types =
    {
        ("conjunction", "standard"),
        ("disjunction", "or"),
        ("unit", "unit"),
    };

    // ECMA-402 style -> CLDR key suffix.
    private static readonly (string EcmaStyle, string Suffix)[] Styles =
    {
        ("long", ""),
        ("short", "-short"),
        ("narrow", "-narrow"),
    };

    /// <summary>Parses every <c>&lt;locale&gt;/listPatterns.json</c> under <paramref name="dataDirectory"/>.</summary>
    public static IReadOnlyList<LocaleListPatterns> ParseAll(string dataDirectory)
    {
        var result = new List<LocaleListPatterns>();
        foreach (var dir in Directory.EnumerateDirectories(dataDirectory).OrderBy(static d => d, StringComparer.Ordinal))
        {
            var file = Path.Combine(dir, "listPatterns.json");
            if (!File.Exists(file))
            {
                continue;
            }

            var parsed = ParseFile(file);
            if (parsed is not null)
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    private static LocaleListPatterns? ParseFile(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("main", out var main))
        {
            return null;
        }

        using var localeEnumerator = main.EnumerateObject().GetEnumerator();
        if (!localeEnumerator.MoveNext())
        {
            return null;
        }

        var localeProperty = localeEnumerator.Current;
        if (!localeProperty.Value.TryGetProperty("listPatterns", out var listPatterns))
        {
            return null;
        }

        var byTypeStyle = new Dictionary<string, CldrListPattern>(StringComparer.Ordinal);
        foreach (var (ecmaType, cldrType) in Types)
        {
            foreach (var (ecmaStyle, suffix) in Styles)
            {
                var key = $"listPattern-type-{cldrType}{suffix}";
                if (!listPatterns.TryGetProperty(key, out var element))
                {
                    continue;
                }

                byTypeStyle[$"{ecmaType}|{ecmaStyle}"] = new CldrListPattern(
                    element.GetProperty("start").GetString()!,
                    element.GetProperty("middle").GetString()!,
                    element.GetProperty("end").GetString()!,
                    element.GetProperty("2").GetString()!);
            }
        }

        var language = localeProperty.Name.ToLowerInvariant();
        return new LocaleListPatterns(language, byTypeStyle);
    }
}
