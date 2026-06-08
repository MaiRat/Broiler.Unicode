namespace UnicodeCldr.LocaleData;

/// <summary>
/// A locale-aware description of how a currency amount is laid out, sufficient to
/// drive an ECMA-402 <c>Intl.NumberFormat</c> <c>style: "currency"</c> formatter:
/// the symbol string, where it sits relative to the number, any literal that
/// separates them, and the negative convention. This is a compact CLDR subset
/// covering the common locales/currencies; unknown currencies fall back to the
/// currency code and the default (symbol-before, parenthesized) layout.
/// </summary>
public readonly struct CldrCurrencyFormat
{
    /// <summary>The rendered currency symbol (e.g. <c>"$"</c>, <c>"US$"</c>, or the code itself).</summary>
    public string Symbol { get; init; }

    /// <summary>When true the symbol follows the number (e.g. de-DE <c>987,00 €</c>); otherwise it precedes it.</summary>
    public bool SymbolAfterNumber { get; init; }

    /// <summary>
    /// Literal placed between the number and the symbol. CLDR uses a no-break space
    /// (U+00A0) for locales such as de-DE; empty when the symbol abuts the number.
    /// </summary>
    public string SpacingBetweenNumberAndSymbol { get; init; }

    /// <summary>
    /// When true an "accounting" negative is wrapped in parentheses (en/ja/ko/zh);
    /// when false the locale uses a leading minus sign instead (de-DE).
    /// </summary>
    public bool AccountingUsesParentheses { get; init; }

    /// <summary>The default number of fraction digits for the currency (USD 2, JPY/KRW 0, BHD/KWD 3, ...).</summary>
    public int FractionDigits { get; init; }
}
