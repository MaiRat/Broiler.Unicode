namespace UnicodeCldr.LocaleData;

/// <summary>
/// Managed CLDR locale data for ECMA-402 <c>Intl</c> formatters, with no ICU or
/// native dependency. This first slice covers the number/currency symbols and the
/// currency layout that the .NET globalization (ICU) APIs do not surface in an
/// Intl-shaped form. It is a hand-curated subset; the longer-term plan is to feed
/// these tables from the official <c>cldr-json</c> data via a generator, mirroring
/// the emoji trie pipeline in this repository.
/// </summary>
public static class CldrLocaleData
{
    /// <summary>The infinity symbol used by every CLDR locale's number system.</summary>
    public const string InfinitySymbol = "∞"; // ∞
    /// <summary>
    /// The primary language subtag of a BCP-47 locale tag, lower-cased
    /// (e.g. <c>"zh-Hant-TW"</c> → <c>"zh"</c>). Returns the empty string for a
    /// null/empty tag.
    /// </summary>
    public static string LanguageOf(string localeTag)
    {
        if (string.IsNullOrEmpty(localeTag))
            return string.Empty;

        var dash = localeTag.IndexOf('-');
        return (dash < 0 ? localeTag : localeTag[..dash]).ToLowerInvariant();
    }

    /// <summary>
    /// The locale's symbol for "not a number" (<c>"NaN"</c> for most locales,
    /// <c>"非數值"</c> for Chinese).
    /// </summary>
    public static string NaNSymbol(string localeTag)
        => LanguageOf(localeTag) == "zh" ? "非數值" : "NaN";

    /// <summary>
    /// The CLDR <c>minimumGroupingDigits</c> for a locale: the minimum number of
    /// digits the most-significant (leading) integer group must contain before any
    /// grouping separators are shown. Most locales use <c>1</c>; a few (Spanish,
    /// Italian, Polish) use <c>2</c>, which suppresses grouping for four-digit
    /// values such as <c>1000</c> (formatted as <c>"1000"</c>, not <c>"1 000"</c>).
    /// Only the language subtag is consulted.
    /// </summary>
    public static int MinimumGroupingDigits(string localeTag)
        => LanguageOf(localeTag) switch
        {
            "es" or "it" or "pl" => 2,
            _ => 1,
        };

    /// <summary>
    /// The CLDR list-assembly patterns for a locale, type and style, used to drive
    /// an ECMA-402 <c>Intl.ListFormat</c> formatter. Falls back to English and then
    /// to a plain comma layout for an unknown locale. The data is generated from the
    /// official cldr-json list patterns (see the generated <c>CldrListData</c>).
    /// </summary>
    /// <param name="localeTag">A BCP-47 locale tag; only the language subtag is used.</param>
    /// <param name="type">ECMA-402 list type: <c>"conjunction"</c>, <c>"disjunction"</c> or <c>"unit"</c>.</param>
    /// <param name="style">ECMA-402 list style: <c>"long"</c>, <c>"short"</c> or <c>"narrow"</c>.</param>
    public static (string Start, string Middle, string End, string Pair) GetListPattern(string localeTag, string type, string style)
    {
        var language = LanguageOf(localeTag);
        if (CldrListData.Patterns.TryGetValue($"{language}|{type}|{style}", out var p)
            || CldrListData.Patterns.TryGetValue($"en|{type}|{style}", out p))
        {
            return (p[0], p[1], p[2], p[3]);
        }

        return ("{0}, {1}", "{0}, {1}", "{0}, {1}", "{0}, {1}");
    }

    /// <summary>
    /// Selects the CLDR plural category of a number for a locale, per ECMA-402
    /// <c>Intl.PluralRules.prototype.select</c>. The rules are generated from the
    /// official cldr-json plural data (see the generated <c>CldrPluralData</c>);
    /// an unknown locale resolves to <c>"other"</c>.
    /// </summary>
    /// <param name="localeTag">A BCP-47 locale tag.</param>
    /// <param name="type"><c>"cardinal"</c> or <c>"ordinal"</c>.</param>
    /// <param name="number">The number to categorize.</param>
    /// <param name="minimumFractionDigits">Minimum visible fraction digits (default 0).</param>
    /// <param name="maximumFractionDigits">Maximum visible fraction digits (default 3).</param>
    public static string SelectPlural(string localeTag, string type, double number, int minimumFractionDigits = 0, int maximumFractionDigits = 3)
        => CldrPluralRules.Select(localeTag, type, number, minimumFractionDigits, maximumFractionDigits);

    /// <summary>
    /// The ordered plural categories a locale uses for a type (always ending in
    /// <c>"other"</c>), for <c>resolvedOptions().pluralCategories</c>.
    /// </summary>
    public static string[] GetPluralCategories(string localeTag, string type)
        => CldrPluralRules.Categories(localeTag, type);

    /// <summary>
    /// The CLDR unit pattern (with a <c>{0}</c> placeholder) for a locale, ECMA-402
    /// simple unit, display width and plural category — used to drive an
    /// <c>Intl.NumberFormat</c> <c>style: "unit"</c> formatter. Falls back to the
    /// "other" category, then English, then a plain <c>"{0} &lt;unit&gt;"</c> layout.
    /// The patterns are generated from the official cldr-json unit data.
    /// </summary>
    /// <param name="localeTag">A BCP-47 locale tag; only the language subtag is used.</param>
    /// <param name="unit">An ECMA-402 simple unit identifier (e.g. <c>"meter"</c>).</param>
    /// <param name="display"><c>"long"</c>, <c>"short"</c> or <c>"narrow"</c>.</param>
    /// <param name="pluralCategory">The plural category of the number (e.g. <c>"one"</c>/<c>"other"</c>).</param>
    public static string GetUnitPattern(string localeTag, string unit, string display, string pluralCategory)
    {
        var language = LanguageOf(localeTag);
        return CldrUnitData.Patterns.GetValueOrDefault($"{language}|{unit}|{display}|{pluralCategory}")
            ?? CldrUnitData.Patterns.GetValueOrDefault($"{language}|{unit}|{display}|other")
            ?? CldrUnitData.Patterns.GetValueOrDefault($"en|{unit}|{display}|{pluralCategory}")
            ?? CldrUnitData.Patterns.GetValueOrDefault($"en|{unit}|{display}|other")
            ?? $"{{0}} {unit}";
    }

    /// <summary>
    /// The CLDR relative-time pattern (with a <c>{0}</c> placeholder) for a locale,
    /// ECMA-402 unit, style, tense and plural category — used to drive an
    /// <c>Intl.RelativeTimeFormat</c> formatter with <c>numeric: "always"</c>. Falls
    /// back to the "other" category, then English, then a bare <c>"{0}"</c>. The
    /// patterns are generated from the official cldr-json relative-time data.
    /// </summary>
    /// <param name="localeTag">A BCP-47 locale tag; only the language subtag is used.</param>
    /// <param name="unit">An ECMA-402 relative-time unit (e.g. <c>"day"</c>, <c>"year"</c>).</param>
    /// <param name="style"><c>"long"</c>, <c>"short"</c> or <c>"narrow"</c>.</param>
    /// <param name="tense"><c>"future"</c> or <c>"past"</c>.</param>
    /// <param name="pluralCategory">The plural category of the number (e.g. <c>"one"</c>/<c>"other"</c>).</param>
    public static string GetRelativeTimePattern(string localeTag, string unit, string style, string tense, string pluralCategory)
    {
        var language = LanguageOf(localeTag);
        return CldrRelativeTimeData.Patterns.GetValueOrDefault($"{language}|{unit}|{style}|{tense}|{pluralCategory}")
            ?? CldrRelativeTimeData.Patterns.GetValueOrDefault($"{language}|{unit}|{style}|{tense}|other")
            ?? CldrRelativeTimeData.Patterns.GetValueOrDefault($"en|{unit}|{style}|{tense}|{pluralCategory}")
            ?? CldrRelativeTimeData.Patterns.GetValueOrDefault($"en|{unit}|{style}|{tense}|other")
            ?? "{0}";
    }

    /// <summary>
    /// The CLDR exact relative phrase (e.g. <c>"yesterday"</c>) for a locale, unit,
    /// style and signed offset, used by <c>Intl.RelativeTimeFormat</c> with
    /// <c>numeric: "auto"</c>. Returns <see langword="null"/> when the offset has no
    /// exact phrase (the caller then falls back to the numeric pattern).
    /// </summary>
    /// <param name="localeTag">A BCP-47 locale tag; only the language subtag is used.</param>
    /// <param name="unit">An ECMA-402 relative-time unit (e.g. <c>"day"</c>).</param>
    /// <param name="style"><c>"long"</c>, <c>"short"</c> or <c>"narrow"</c>.</param>
    /// <param name="offset">The signed numeric offset (e.g. <c>-1</c> for "yesterday").</param>
    public static string? GetRelativeTimeExact(string localeTag, string unit, string style, int offset)
    {
        var language = LanguageOf(localeTag);
        return CldrRelativeTimeData.Exact.GetValueOrDefault($"{language}|{unit}|{style}|{offset}")
            ?? CldrRelativeTimeData.Exact.GetValueOrDefault($"en|{unit}|{style}|{offset}");
    }

    /// <summary>
    /// The day-period key (e.g. <c>"midnight"</c>, <c>"morning1"</c>, <c>"noon"</c>,
    /// <c>"pm"</c>) for a local time, using the locale's CLDR day-period rules. Falls
    /// back to English rules and then to plain am/pm.
    /// </summary>
    public static string GetDayPeriod(string localeTag, int hour, int minute)
        => CldrDayPeriods.Resolve(localeTag, hour, minute);

    /// <summary>
    /// The localized day-period name for a width, used by
    /// <c>Intl.DateTimeFormat</c>'s <c>dayPeriod</c> option. The ECMA-402 widths map
    /// to CLDR widths long→wide, short→abbreviated, narrow→narrow.
    /// </summary>
    /// <param name="localeTag">A BCP-47 locale tag; only the language subtag is used.</param>
    /// <param name="period">A day-period key from <see cref="GetDayPeriod"/>.</param>
    /// <param name="width">A CLDR width: <c>"wide"</c>, <c>"abbreviated"</c> or <c>"narrow"</c>.</param>
    public static string GetDayPeriodName(string localeTag, string period, string width)
        => CldrDayPeriods.Name(localeTag, period, width);

    /// <summary>
    /// The localized time-zone display name for an IANA zone, used by the non-offset styles of
    /// <c>Intl.DateTimeFormat</c>'s <c>timeZoneName</c> option. Returns <c>null</c> when the
    /// locale (only English is bundled), zone or style is not covered, so the caller can fall
    /// back to the GMT offset.
    /// </summary>
    /// <param name="localeTag">A BCP-47 locale tag; only the language subtag is used.</param>
    /// <param name="ianaZone">An IANA time-zone id (e.g. <c>"Europe/Vienna"</c>).</param>
    /// <param name="style">An ECMA-402 style: <c>"long"</c>, <c>"short"</c>, <c>"longGeneric"</c> or <c>"shortGeneric"</c>.</param>
    /// <param name="isDaylight">Whether daylight-saving time is in effect at the instant.</param>
    public static string? GetTimeZoneName(string localeTag, string ianaZone, string style, bool isDaylight)
        => CldrTimeZones.Name(localeTag, ianaZone, style, isDaylight);

    /// <summary>
    /// Resolves the currency layout for a locale.
    /// </summary>
    /// <param name="localeTag">A BCP-47 locale tag (e.g. <c>"de-DE"</c>).</param>
    /// <param name="currencyCode">The ISO 4217 currency code (e.g. <c>"USD"</c>); case-insensitive.</param>
    /// <param name="currencyDisplay">
    /// The ECMA-402 <c>currencyDisplay</c> option: <c>"symbol"</c>, <c>"narrowSymbol"</c>,
    /// <c>"code"</c> or <c>"name"</c>. Only <c>"code"</c> and the symbol variants affect
    /// the rendered <see cref="CldrCurrencyFormat.Symbol"/> here.
    /// </param>
    public static CldrCurrencyFormat ResolveCurrency(string localeTag, string currencyCode, string currencyDisplay)
    {
        var code = (currencyCode ?? string.Empty).ToUpperInvariant();
        var language = LanguageOf(localeTag);

        var layout = CldrCurrencyData.Layout.TryGetValue(language, out var l)
            || CldrCurrencyData.Layout.TryGetValue("en", out l)
            ? l
            : null;

        return new CldrCurrencyFormat
        {
            Symbol = ResolveCurrencySymbol(language, code, currencyDisplay),
            SymbolAfterNumber = layout is not null && layout[0] == "1",
            SpacingBetweenNumberAndSymbol = layout is null ? string.Empty : layout[1],
            AccountingUsesParentheses = layout is null || layout[2] == "1",
            FractionDigits = CldrCurrencyData.Digits.TryGetValue(code, out var digits) ? digits : 2,
        };
    }

    private static string ResolveCurrencySymbol(string language, string code, string display)
    {
        if (display == "code")
        {
            return code;
        }

        var variant = display == "narrowSymbol" ? "narrow" : "symbol";
        return LookupCurrencySymbol(language, code, variant)
            ?? LookupCurrencySymbol("en", code, variant)
            ?? code;
    }

    private static string? LookupCurrencySymbol(string language, string code, string variant)
    {
        if (CldrCurrencyData.Symbols.TryGetValue($"{language}|{code}|{variant}", out var symbol))
        {
            return symbol;
        }

        // A missing narrow symbol falls back to the standard symbol.
        if (variant == "narrow" && CldrCurrencyData.Symbols.TryGetValue($"{language}|{code}|symbol", out symbol))
        {
            return symbol;
        }

        return null;
    }
}
