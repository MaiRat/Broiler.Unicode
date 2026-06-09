using System.Text.Json;

namespace UnicodeCldr.LocaleData.Generator;

/// <summary>One CLDR unit pattern for a (locale, display, unit, plural count).</summary>
/// <param name="Locale">The CLDR locale id (lowercased).</param>
/// <param name="Display">The width: <c>"long"</c>, <c>"short"</c> or <c>"narrow"</c>.</param>
/// <param name="Unit">The ECMA-402 simple unit identifier (CLDR subtype, e.g. <c>"meter"</c>).</param>
/// <param name="Count">The plural category (e.g. <c>"one"</c>, <c>"other"</c>).</param>
/// <param name="Pattern">The pattern with a <c>{0}</c> placeholder (e.g. <c>"{0} m"</c>).</param>
public sealed record UnitPattern(string Locale, string Display, string Unit, string Count, string Pattern);

/// <summary>
/// Parses CLDR <c>units.json</c> into per-(locale, display, unit, count) patterns,
/// limited to the ECMA-402 sanctioned simple units.
/// </summary>
public static class CldrUnitsParser
{
    /// <summary>The ECMA-402 sanctioned simple unit identifiers (CLDR subtypes).</summary>
    public static readonly HashSet<string> CuratedUnits = new(StringComparer.Ordinal)
    {
        "acre", "bit", "byte", "celsius", "centimeter", "day", "degree", "fahrenheit",
        "fluid-ounce", "foot", "gallon", "gigabit", "gigabyte", "gram", "hectare",
        "hour", "inch", "kilobit", "kilobyte", "kilogram", "kilometer", "liter",
        "megabit", "megabyte", "meter", "microsecond", "mile", "mile-scandinavian",
        "milliliter", "millimeter", "millisecond", "minute", "month", "nanosecond",
        "ounce", "percent", "petabyte", "pound", "second", "stone", "terabit",
        "terabyte", "week", "yard", "year",
        // ECMA-402 sanctioned compound units that CLDR precomputes; kept as whole
        // identifiers (the category prefix is stripped, so "speed-kilometer-per-hour"
        // becomes "kilometer-per-hour") so the precomputed locale form (e.g. "km/h",
        // "時速 {0} キロメートル") is used rather than a perUnitPattern composition.
        "kilometer-per-hour", "mile-per-hour", "mile-per-gallon",
    };

    private static readonly string[] Displays = { "long", "short", "narrow" };
    private const string CountPrefix = "unitPattern-count-";

    public static IReadOnlyList<UnitPattern> ParseAll(string dataDirectory)
    {
        var result = new List<UnitPattern>();
        foreach (var dir in Directory.EnumerateDirectories(dataDirectory).OrderBy(static d => d, StringComparer.Ordinal))
        {
            var file = Path.Combine(dir, "units.json");
            if (File.Exists(file))
            {
                ParseFile(file, Path.GetFileName(dir).ToLowerInvariant(), result);
            }
        }

        return result;
    }

    private static void ParseFile(string path, string locale, List<UnitPattern> result)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("main", out var main))
        {
            return;
        }

        using var localeEnumerator = main.EnumerateObject().GetEnumerator();
        if (!localeEnumerator.MoveNext() || !localeEnumerator.Current.Value.TryGetProperty("units", out var units))
        {
            return;
        }

        foreach (var display in Displays)
        {
            if (!units.TryGetProperty(display, out var section))
            {
                continue;
            }

            foreach (var entry in section.EnumerateObject())
            {
                // entry.Name is "<category>-<subtype>" (e.g. "length-meter").
                var dash = entry.Name.IndexOf('-');
                if (dash < 0)
                {
                    continue;
                }

                var unit = entry.Name[(dash + 1)..];
                if (!CuratedUnits.Contains(unit))
                {
                    continue;
                }

                foreach (var property in entry.Value.EnumerateObject())
                {
                    if (!property.Name.StartsWith(CountPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var count = property.Name[CountPrefix.Length..];
                    result.Add(new UnitPattern(locale, display, unit, count, property.Value.GetString()!));
                }
            }
        }
    }
}
