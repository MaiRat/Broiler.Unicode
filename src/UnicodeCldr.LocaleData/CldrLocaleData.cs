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

    private const string NoBreakSpace = " ";

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

        // de-DE places the symbol after the number, separated by a no-break space,
        // and uses a leading minus for accounting negatives; the other covered
        // locales place it before the number and wrap accounting negatives in
        // parentheses.
        var symbolAfter = language == "de";

        return new CldrCurrencyFormat
        {
            Symbol = currencyDisplay == "code" ? code : CurrencySymbol(code, language, currencyDisplay),
            SymbolAfterNumber = symbolAfter,
            SpacingBetweenNumberAndSymbol = symbolAfter ? NoBreakSpace : string.Empty,
            AccountingUsesParentheses = !symbolAfter,
            FractionDigits = CurrencyDigits(code),
        };
    }

    private static string CurrencySymbol(string code, string language, string display)
    {
        var wide = display != "narrowSymbol" && (language is "ko" or "zh");
        return code switch
        {
            "USD" => wide ? "US$" : "$",
            "JPY" => wide ? "JP¥" : "¥",
            "EUR" => "€",
            "GBP" => "£",
            _ => code,
        };
    }

    private static int CurrencyDigits(string code) => code switch
    {
        "JPY" or "KRW" or "CLP" or "VND" => 0,
        "BHD" or "KWD" or "OMR" or "TND" => 3,
        _ => 2,
    };
}
