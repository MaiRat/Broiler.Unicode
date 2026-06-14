namespace UnicodeCldr.LocaleData;

/// <summary>
/// Resolves localized time-zone display names from the generated <c>CldrTimeZoneData</c>
/// tables, for the non-offset styles of <c>Intl.DateTimeFormat</c>'s <c>timeZoneName</c>
/// option (long / short / longGeneric / shortGeneric). Offset styles are produced by the
/// caller from the UTC offset; only English metazone names are bundled, so other locales
/// (and uncovered zones) return <c>null</c> and the caller falls back to the GMT offset.
/// </summary>
internal static class CldrTimeZones
{
    /// <summary>
    /// The display name for an IANA zone at an instant, or <c>null</c> when the locale, zone or
    /// style is not covered.
    /// </summary>
    /// <param name="localeTag">A BCP-47 locale tag; only the language subtag is used.</param>
    /// <param name="ianaZone">An IANA time-zone id (e.g. <c>"Europe/Vienna"</c>).</param>
    /// <param name="style">An ECMA-402 timeZoneName style: long / short / longGeneric / shortGeneric.</param>
    /// <param name="isDaylight">Whether daylight-saving time is in effect at the instant.</param>
    public static string? Name(string localeTag, string ianaZone, string style, bool isDaylight)
    {
        if (ianaZone is null || CldrLocaleData.LanguageOf(localeTag) is not "en")
            return null; // only English metazone names are bundled
        if (!CldrTimeZoneData.ZoneToMetazone.TryGetValue(ianaZone, out var metazone))
            return null;

        string? width = null, type = null;
        switch (style)
        {
            case "long": width = "long"; type = isDaylight ? "daylight" : "standard"; break;
            case "short": width = "short"; type = isDaylight ? "daylight" : "standard"; break;
            case "longGeneric": width = "long"; type = "generic"; break;
            case "shortGeneric": width = "short"; type = "generic"; break;
        }
        if (width is null)
            return null;

        // Specific (standard/daylight) names fall back to the generic name when absent.
        return CldrTimeZoneData.MetazoneNames.GetValueOrDefault($"en|{metazone}|{width}|{type}")
            ?? CldrTimeZoneData.MetazoneNames.GetValueOrDefault($"en|{metazone}|{width}|generic");
    }
}
