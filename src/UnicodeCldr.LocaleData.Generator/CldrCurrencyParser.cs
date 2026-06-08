using System.Text.Json;

namespace UnicodeCldr.LocaleData.Generator;

/// <summary>How a locale lays out a currency amount, derived from its CLDR accounting pattern.</summary>
/// <param name="Locale">The CLDR locale id (lowercased).</param>
/// <param name="SymbolAfter">Whether the currency symbol follows the number.</param>
/// <param name="Spacing">The literal between number and symbol (often U+00A0, possibly empty).</param>
/// <param name="AccountingParens">Whether the accounting negative wraps in parentheses.</param>
public sealed record CurrencyLayout(string Locale, bool SymbolAfter, string Spacing, bool AccountingParens);

/// <summary>The symbol and narrow symbol of a currency in a locale.</summary>
public sealed record CurrencySymbol(string Locale, string Code, string Symbol, string NarrowSymbol);

/// <summary>The default fraction-digit count of a currency.</summary>
public sealed record CurrencyDigits(string Code, int Digits);

/// <summary>Everything parsed for the currency domain.</summary>
public sealed record CldrCurrencyModel(
    IReadOnlyList<CurrencyLayout> Layouts,
    IReadOnlyList<CurrencySymbol> Symbols,
    IReadOnlyList<CurrencyDigits> Digits);

/// <summary>
/// Parses CLDR currency data: per-locale layout from <c>numbers.json</c>
/// (currencyFormats accounting pattern), per-(locale,currency) symbols from
/// <c>currencies.json</c>, and per-currency fraction digits from the supplemental
/// <c>currencyData.json</c>.
/// </summary>
public static class CldrCurrencyParser
{
    /// <summary>The currencies whose symbols are emitted (keeps the symbol table small).</summary>
    public static readonly string[] CuratedCurrencies =
    {
        "USD", "EUR", "GBP", "JPY", "CNY", "KRW", "INR", "RUB", "BRL", "CAD",
        "AUD", "CHF", "HKD", "MXN", "SEK", "NOK", "DKK", "PLN", "TRY", "NZD",
        "ZAR", "SGD", "THB", "TWD",
    };

    private const char CurrencyPlaceholder = '¤'; // ¤

    public static CldrCurrencyModel ParseAll(string dataDirectory, string supplementalDirectory)
    {
        var layouts = new List<CurrencyLayout>();
        var symbols = new List<CurrencySymbol>();

        foreach (var dir in Directory.EnumerateDirectories(dataDirectory).OrderBy(static d => d, StringComparer.Ordinal))
        {
            var locale = Path.GetFileName(dir).ToLowerInvariant();
            var numbers = Path.Combine(dir, "numbers.json");
            if (File.Exists(numbers) && ParseLayout(numbers, locale) is { } layout)
            {
                layouts.Add(layout);
            }

            var currencies = Path.Combine(dir, "currencies.json");
            if (File.Exists(currencies))
            {
                symbols.AddRange(ParseSymbols(currencies, locale));
            }
        }

        var digits = ParseDigits(Path.Combine(supplementalDirectory, "currencyData.json"));
        return new CldrCurrencyModel(layouts, symbols, digits);
    }

    private static CurrencyLayout? ParseLayout(string path, string locale)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!TryGetLocaleNumbers(document, out var numbers)
            || !numbers.TryGetProperty("currencyFormats-numberSystem-latn", out var formats))
        {
            return null;
        }

        var pattern = formats.TryGetProperty("accounting", out var accounting) ? accounting.GetString()
            : formats.TryGetProperty("standard", out var standard) ? standard.GetString()
            : null;
        if (string.IsNullOrEmpty(pattern))
        {
            return null;
        }

        var semicolon = pattern.IndexOf(';');
        var positive = semicolon < 0 ? pattern : pattern[..semicolon];
        var negative = semicolon < 0 ? null : pattern[(semicolon + 1)..];
        var accountingParens = negative is not null && negative.Contains('(');

        var currency = positive.IndexOf(CurrencyPlaceholder);
        var firstNumber = positive.IndexOfAny(new[] { '#', '0' });
        var lastNumber = positive.LastIndexOfAny(new[] { '#', '0' });
        if (currency < 0 || firstNumber < 0)
        {
            return null;
        }

        bool symbolAfter;
        string spacing;
        if (currency < firstNumber)
        {
            symbolAfter = false;
            spacing = positive[(currency + 1)..firstNumber];
        }
        else
        {
            symbolAfter = true;
            spacing = positive[(lastNumber + 1)..currency];
        }

        return new CurrencyLayout(locale, symbolAfter, spacing, accountingParens);
    }

    private static IEnumerable<CurrencySymbol> ParseSymbols(string path, string locale)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!TryGetLocaleNumbers(document, out var numbers)
            || !numbers.TryGetProperty("currencies", out var currencies))
        {
            yield break;
        }

        foreach (var code in CuratedCurrencies)
        {
            if (!currencies.TryGetProperty(code, out var entry)
                || !entry.TryGetProperty("symbol", out var symbolElement))
            {
                continue;
            }

            var symbol = symbolElement.GetString()!;
            var narrow = entry.TryGetProperty("symbol-alt-narrow", out var narrowElement)
                ? narrowElement.GetString()!
                : symbol;
            yield return new CurrencySymbol(locale, code, symbol, narrow);
        }
    }

    private static IReadOnlyList<CurrencyDigits> ParseDigits(string path)
    {
        var result = new List<CurrencyDigits>();
        if (!File.Exists(path))
        {
            return result;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("supplemental", out var supplemental)
            || !supplemental.TryGetProperty("currencyData", out var currencyData)
            || !currencyData.TryGetProperty("fractions", out var fractions))
        {
            return result;
        }

        foreach (var entry in fractions.EnumerateObject())
        {
            if (entry.Name == "DEFAULT" || !entry.Value.TryGetProperty("_digits", out var digitsElement))
            {
                continue;
            }

            // Only emit currencies that differ from the default (2 digits).
            if (int.TryParse(digitsElement.GetString(), out var digits) && digits != 2)
            {
                result.Add(new CurrencyDigits(entry.Name.ToUpperInvariant(), digits));
            }
        }

        return result;
    }

    private static bool TryGetLocaleNumbers(JsonDocument document, out JsonElement numbers)
    {
        numbers = default;
        if (!document.RootElement.TryGetProperty("main", out var main))
        {
            return false;
        }

        using var localeEnumerator = main.EnumerateObject().GetEnumerator();
        if (!localeEnumerator.MoveNext())
        {
            return false;
        }

        return localeEnumerator.Current.Value.TryGetProperty("numbers", out numbers);
    }
}
