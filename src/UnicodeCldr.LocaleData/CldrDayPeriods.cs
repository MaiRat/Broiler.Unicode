namespace UnicodeCldr.LocaleData;

/// <summary>
/// Resolves CLDR flexible day periods (am/pm plus noon/midnight/morning/…/night)
/// from the generated <c>CldrDateData</c> tables, for ECMA-402
/// <c>Intl.DateTimeFormat</c> day-period formatting.
/// </summary>
internal static class CldrDayPeriods
{
    /// <summary>
    /// The day-period key (e.g. "midnight", "morning1", "noon", "pm") for a local
    /// time, using the locale's day-period rules. Falls back to English rules and
    /// finally to plain am/pm.
    /// </summary>
    public static string Resolve(string localeTag, int hour, int minute)
    {
        var language = CldrLocaleData.LanguageOf(localeTag);
        var rules = CldrDateData.DayPeriodRules.GetValueOrDefault(language)
            ?? CldrDateData.DayPeriodRules.GetValueOrDefault("en");
        var time = hour * 60 + minute;

        if (rules is not null && MatchRules(rules, time) is { } period)
        {
            return period;
        }

        return hour < 12 ? "am" : "pm";
    }

    private static string? MatchRules(string rules, int time)
    {
        // "at" rules are listed first and take precedence over ranges.
        foreach (var rule in rules.Split(';'))
        {
            var at = rule.IndexOf('@');
            if (at >= 0)
            {
                if (int.Parse(rule[(at + 1)..]) == time)
                {
                    return rule[..at];
                }

                continue;
            }

            var eq = rule.IndexOf('=');
            if (eq < 0)
            {
                continue;
            }

            var period = rule[..eq];
            var dash = rule.IndexOf('-', eq + 1);
            var from = int.Parse(rule[(eq + 1)..dash]);
            var before = int.Parse(rule[(dash + 1)..]);

            // A normal half-open range, or a wrapping one (e.g. 21:00–06:00).
            var matched = from <= before
                ? time >= from && time < before
                : time >= from || time < before;
            if (matched)
            {
                return period;
            }
        }

        return null;
    }

    /// <summary>
    /// The localized name of a day period for a CLDR width, falling back to English
    /// and then to the period key.
    /// </summary>
    public static string Name(string localeTag, string period, string width)
    {
        var language = CldrLocaleData.LanguageOf(localeTag);
        return CldrDateData.DayPeriodNames.GetValueOrDefault($"{language}|{period}|{width}")
            ?? CldrDateData.DayPeriodNames.GetValueOrDefault($"en|{period}|{width}")
            ?? period;
    }
}
