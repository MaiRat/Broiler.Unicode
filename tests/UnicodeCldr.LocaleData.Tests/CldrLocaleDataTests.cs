using UnicodeCldr.LocaleData;
using Xunit;

namespace UnicodeCldr.LocaleData.Tests;

public class CldrLocaleDataTests
{
    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("de-DE", "de")]
    [InlineData("zh-Hant-TW", "zh")]
    [InlineData("ja", "ja")]
    [InlineData("", "")]
    public void LanguageOf_ReturnsPrimarySubtag(string tag, string expected)
        => Assert.Equal(expected, CldrLocaleData.LanguageOf(tag));

    [Theory]
    [InlineData("en-US", "NaN")]
    [InlineData("de-DE", "NaN")]
    [InlineData("zh-TW", "非數值")]
    public void NaNSymbol_IsLocaleSpecific(string tag, string expected)
        => Assert.Equal(expected, CldrLocaleData.NaNSymbol(tag));

    [Fact]
    public void InfinitySymbol_IsUniversal()
        => Assert.Equal("∞", CldrLocaleData.InfinitySymbol);

    // en-US/ja-JP: "$" before the number, accounting negatives parenthesized.
    [Theory]
    [InlineData("en-US")]
    [InlineData("ja-JP")]
    public void Currency_UsdSymbolBeforeNumber(string tag)
    {
        var f = CldrLocaleData.ResolveCurrency(tag, "USD", "symbol");
        Assert.Equal("$", f.Symbol);
        Assert.False(f.SymbolAfterNumber);
        Assert.Equal(string.Empty, f.SpacingBetweenNumberAndSymbol);
        Assert.True(f.AccountingUsesParentheses);
        Assert.Equal(2, f.FractionDigits);
    }

    // ko-KR/zh-TW render USD as the wide "US$" symbol.
    [Theory]
    [InlineData("ko-KR")]
    [InlineData("zh-TW")]
    public void Currency_UsdWideSymbol(string tag)
        => Assert.Equal("US$", CldrLocaleData.ResolveCurrency(tag, "USD", "symbol").Symbol);

    // narrowSymbol collapses the wide symbol back to "$".
    [Fact]
    public void Currency_NarrowSymbolIsNarrow()
        => Assert.Equal("$", CldrLocaleData.ResolveCurrency("ko-KR", "USD", "narrowSymbol").Symbol);

    // de-DE: symbol after the number, separated by a no-break space, minus (no parens).
    [Fact]
    public void Currency_DeTrailingSymbolWithNoBreakSpace()
    {
        var f = CldrLocaleData.ResolveCurrency("de-DE", "USD", "symbol");
        Assert.Equal("$", f.Symbol);
        Assert.True(f.SymbolAfterNumber);
        // CLDR uses a no-break space (U+00A0), not an ASCII space.
        var spacing = f.SpacingBetweenNumberAndSymbol;
        Assert.Single(spacing);
        Assert.Equal(0x00A0, (int)spacing[0]);
        Assert.False(f.AccountingUsesParentheses);
    }

    [Theory]
    [InlineData("USD", 2)]
    [InlineData("EUR", 2)]
    [InlineData("JPY", 0)]
    [InlineData("KRW", 0)]
    [InlineData("BHD", 3)]
    public void Currency_FractionDigits(string code, int expected)
        => Assert.Equal(expected, CldrLocaleData.ResolveCurrency("en-US", code, "symbol").FractionDigits);

    [Fact]
    public void Currency_DisplayCodeReturnsCode()
        => Assert.Equal("USD", CldrLocaleData.ResolveCurrency("en-US", "usd", "code").Symbol);

    [Fact]
    public void Currency_UnknownCurrencyFallsBackToCode()
        => Assert.Equal("XTS", CldrLocaleData.ResolveCurrency("en-US", "XTS", "symbol").Symbol);

    // ---- List patterns (generated from cldr-json) ----

    [Fact]
    public void ListPattern_EnglishConjunctionLong()
    {
        var (start, middle, end, pair) = CldrLocaleData.GetListPattern("en-US", "conjunction", "long");
        Assert.Equal("{0}, {1}", start);
        Assert.Equal("{0}, {1}", middle);
        Assert.Equal("{0}, and {1}", end);
        Assert.Equal("{0} and {1}", pair);
    }

    [Fact]
    public void ListPattern_EnglishConjunctionShortUsesAmpersand()
        => Assert.Equal("{0}, & {1}", CldrLocaleData.GetListPattern("en-US", "conjunction", "short").End);

    [Fact]
    public void ListPattern_SpanishUnitUsesY()
        => Assert.Equal("{0} y {1}", CldrLocaleData.GetListPattern("es-ES", "unit", "long").End);

    [Fact]
    public void ListPattern_SpanishUnitNarrowIsSpaceSeparated()
        => Assert.Equal("{0} {1}", CldrLocaleData.GetListPattern("es-ES", "unit", "narrow").Pair);

    // zh uses the ideographic comma; coverage comes from the generated data.
    [Fact]
    public void ListPattern_ChineseUsesIdeographicComma()
        => Assert.Equal("{0}、{1}", CldrLocaleData.GetListPattern("zh-TW", "conjunction", "long").Start);

    // An unknown locale falls back to English patterns.
    [Fact]
    public void ListPattern_UnknownLocaleFallsBackToEnglish()
        => Assert.Equal(
            CldrLocaleData.GetListPattern("en", "conjunction", "long"),
            CldrLocaleData.GetListPattern("xx-YY", "conjunction", "long"));
}
