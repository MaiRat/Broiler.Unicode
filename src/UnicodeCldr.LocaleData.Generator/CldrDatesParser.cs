using System.Text.Json;

namespace UnicodeCldr.LocaleData.Generator;

/// <summary>A localized day-period name for a (locale, width, period).</summary>
/// <param name="Locale">The CLDR locale id (lowercased).</param>
/// <param name="Width">The CLDR width: <c>"wide"</c>, <c>"abbreviated"</c> or <c>"narrow"</c>.</param>
/// <param name="Period">The day-period key (e.g. <c>"am"</c>, <c>"noon"</c>, <c>"morning1"</c>).</param>
/// <param name="Name">The localized name (e.g. <c>"in the morning"</c>).</param>
public sealed record DayPeriodName(string Locale, string Width, string Period, string Name);

/// <summary>The encoded day-period rules for a locale.</summary>
/// <param name="Locale">The CLDR locale id (lowercased).</param>
/// <param name="Rules">
/// <c>;</c>-separated rules: <c>period@minutes</c> for an exact time, or
/// <c>period=from-before</c> for a half-open minute range.
/// </param>
public sealed record DayPeriodRuleSet(string Locale, string Rules);

/// <summary>Everything parsed for the date domain (currently day periods).</summary>
public sealed record CldrDatesModel(IReadOnlyList<DayPeriodName> Names, IReadOnlyList<DayPeriodRuleSet> Rules);

/// <summary>
/// Parses CLDR date data: day-period names from each locale's
/// <c>ca-gregorian.json</c> and the day-period rules from the supplemental
/// <c>dayPeriods.json</c>.
/// </summary>
public static class CldrDatesParser
{
    private static readonly string[] Widths = { "wide", "abbreviated", "narrow" };

    public static CldrDatesModel ParseAll(string dataDirectory, string supplementalDirectory)
    {
        var names = new List<DayPeriodName>();
        foreach (var dir in Directory.EnumerateDirectories(dataDirectory).OrderBy(static d => d, StringComparer.Ordinal))
        {
            var file = Path.Combine(dir, "ca-gregorian.json");
            if (File.Exists(file))
            {
                ParseNames(file, Path.GetFileName(dir).ToLowerInvariant(), names);
            }
        }

        var rules = ParseRules(Path.Combine(supplementalDirectory, "dayPeriods.json"));
        return new CldrDatesModel(names, rules);
    }

    private static void ParseNames(string path, string locale, List<DayPeriodName> names)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("main", out var main))
        {
            return;
        }

        using var localeEnumerator = main.EnumerateObject().GetEnumerator();
        if (!localeEnumerator.MoveNext()
            || !localeEnumerator.Current.Value.TryGetProperty("dates", out var dates)
            || !dates.TryGetProperty("calendars", out var calendars)
            || !calendars.TryGetProperty("gregorian", out var gregorian)
            || !gregorian.TryGetProperty("dayPeriods", out var dayPeriods)
            || !dayPeriods.TryGetProperty("format", out var format))
        {
            return;
        }

        foreach (var width in Widths)
        {
            if (!format.TryGetProperty(width, out var section))
            {
                continue;
            }

            foreach (var period in section.EnumerateObject())
            {
                // Skip the "-alt-variant" lowercase am/pm aliases.
                if (period.Name.Contains("-alt-", StringComparison.Ordinal))
                {
                    continue;
                }

                names.Add(new DayPeriodName(locale, width, period.Name, period.Value.GetString()!));
            }
        }
    }

    private static IReadOnlyList<DayPeriodRuleSet> ParseRules(string path)
    {
        var result = new List<DayPeriodRuleSet>();
        if (!File.Exists(path))
        {
            return result;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("supplemental", out var supplemental)
            || !supplemental.TryGetProperty("dayPeriodRuleSet", out var ruleSet))
        {
            return result;
        }

        foreach (var locale in ruleSet.EnumerateObject())
        {
            var ats = new List<string>();
            var ranges = new List<string>();
            foreach (var period in locale.Value.EnumerateObject())
            {
                if (period.Value.TryGetProperty("_at", out var at))
                {
                    ats.Add($"{period.Name}@{ToMinutes(at.GetString()!)}");
                }
                else if (period.Value.TryGetProperty("_from", out var from)
                    && period.Value.TryGetProperty("_before", out var before))
                {
                    ranges.Add($"{period.Name}={ToMinutes(from.GetString()!)}-{ToMinutes(before.GetString()!)}");
                }
            }

            // "at" rules take precedence, so list them first.
            var rules = string.Join(";", ats.Concat(ranges));
            if (rules.Length > 0)
            {
                result.Add(new DayPeriodRuleSet(locale.Name.ToLowerInvariant().Replace('_', '-'), rules));
            }
        }

        return result;
    }

    private static int ToMinutes(string hhmm)
    {
        var colon = hhmm.IndexOf(':');
        var hours = int.Parse(hhmm[..colon]);
        var minutes = int.Parse(hhmm[(colon + 1)..]);
        return hours * 60 + minutes;
    }
}
