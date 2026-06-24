using System.Globalization;

namespace UnicodeCldr.LocaleData;

/// <summary>
/// Evaluates CLDR plural rules (UTS #35 Part 3) against a number to obtain its
/// plural category. The rule conditions are stored as strings in the generated
/// <c>CldrPluralData</c> table and parsed on demand; this is not a hot path.
/// </summary>
internal static class CldrPluralRules
{
    /// <summary>Returns the plural category (e.g. "one"/"other") for a number.</summary>
    public static string Select(string localeTag, string type, double number, int minFractionDigits, int maxFractionDigits, int exponent = 0)
    {
        if (!double.IsFinite(number))
        {
            return "other";
        }

        if (!TryGetRules(localeTag, type, out var flat))
        {
            return "other";
        }

        var operands = Operands.Compute(number, minFractionDigits, maxFractionDigits, exponent);
        for (var k = 0; k + 1 < flat.Length; k += 2)
        {
            if (Evaluate(flat[k + 1], operands))
            {
                return flat[k];
            }
        }

        return "other";
    }

    /// <summary>The ordered plural categories a locale uses (always ending in "other").</summary>
    public static string[] Categories(string localeTag, string type)
    {
        if (!TryGetRules(localeTag, type, out var flat))
        {
            return new[] { "other" };
        }

        var categories = new string[flat.Length / 2 + 1];
        for (var k = 0; k < flat.Length; k += 2)
        {
            categories[k / 2] = flat[k];
        }

        categories[^1] = "other";
        return categories;
    }

    private static bool TryGetRules(string localeTag, string type, out string[] flat)
    {
        var lower = (localeTag ?? string.Empty).ToLowerInvariant();
        if (CldrPluralData.Rules.TryGetValue($"{lower}|{type}", out flat!))
        {
            return true;
        }

        var language = CldrLocaleData.LanguageOf(localeTag ?? string.Empty);
        return CldrPluralData.Rules.TryGetValue($"{language}|{type}", out flat!);
    }

    // ---- Rule grammar evaluation ----
    // condition     = and_condition ('or' and_condition)*
    // and_condition = relation ('and' relation)*
    // relation      = expr ('=' | '!=') range_list
    // expr          = operand ('%' value)?
    // range_list    = (value | value '..' value) (',' ...)*

    private static bool Evaluate(string condition, in Operands op)
    {
        condition = condition.Trim();
        if (condition.Length == 0)
        {
            return true;
        }

        foreach (var orTerm in condition.Split(" or ", StringSplitOptions.None))
        {
            if (EvaluateAnd(orTerm, op))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EvaluateAnd(string term, in Operands op)
    {
        foreach (var relation in term.Split(" and ", StringSplitOptions.None))
        {
            if (!EvaluateRelation(relation.Trim(), op))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EvaluateRelation(string relation, in Operands op)
    {
        bool negate;
        int opIndex;
        int opLength;
        if ((opIndex = relation.IndexOf(" != ", StringComparison.Ordinal)) >= 0)
        {
            negate = true;
            opLength = 4;
        }
        else if ((opIndex = relation.IndexOf(" = ", StringComparison.Ordinal)) >= 0)
        {
            negate = false;
            opLength = 3;
        }
        else
        {
            return false; // unrecognized — treat as non-matching
        }

        var left = relation[..opIndex].Trim();
        var right = relation[(opIndex + opLength)..].Trim();

        var value = EvaluateExpression(left, op);
        var matched = MatchesRangeList(value, right);
        return negate ? !matched : matched;
    }

    private static double EvaluateExpression(string expr, in Operands op)
    {
        var percent = expr.IndexOf('%');
        if (percent < 0)
        {
            return OperandValue(expr.Trim(), op);
        }

        var operand = OperandValue(expr[..percent].Trim(), op);
        var modulus = double.Parse(expr[(percent + 1)..].Trim(), CultureInfo.InvariantCulture);
        return modulus == 0 ? operand : operand % modulus;
    }

    private static double OperandValue(string operand, in Operands op) => operand switch
    {
        "n" => op.N,
        "i" => op.I,
        "v" => op.V,
        "w" => op.W,
        "f" => op.F,
        "t" => op.T,
        "e" => op.E,
        "c" => op.C,
        _ => double.NaN,
    };

    private static bool MatchesRangeList(double value, string rangeList)
    {
        foreach (var item in rangeList.Split(','))
        {
            var part = item.Trim();
            var dots = part.IndexOf("..", StringComparison.Ordinal);
            if (dots >= 0)
            {
                var low = double.Parse(part[..dots], CultureInfo.InvariantCulture);
                var high = double.Parse(part[(dots + 2)..], CultureInfo.InvariantCulture);
                // A range matches integers only (UTS #35 "in" semantics).
                if (value >= low && value <= high && value == Math.Truncate(value))
                {
                    return true;
                }
            }
            else if (value == double.Parse(part, CultureInfo.InvariantCulture))
            {
                return true;
            }
        }

        return false;
    }

    // The plural operands derived from a number formatted with the given fraction
    // digit bounds (UTS #35: n, i, v, w, f, t; c/e are the compact/scientific exponent).
    private readonly struct Operands
    {
        public double N { get; private init; }
        public long I { get; private init; }
        public int V { get; private init; }
        public int W { get; private init; }
        public long F { get; private init; }
        public long T { get; private init; }
        public int E { get; private init; }
        public int C { get; private init; }

        public static Operands Compute(double number, int minFractionDigits, int maxFractionDigits, int exponent = 0)
        {
            var abs = Math.Abs(number);
            var max = Math.Clamp(maxFractionDigits, 0, 15);
            var formatted = abs.ToString("F" + max, CultureInfo.InvariantCulture);

            var dot = formatted.IndexOf('.');
            var intPart = dot < 0 ? formatted : formatted[..dot];
            var fraction = dot < 0 ? string.Empty : formatted[(dot + 1)..];

            // Trim trailing zeros down to the minimum requested fraction digits.
            while (fraction.Length > minFractionDigits && fraction.EndsWith('0'))
            {
                fraction = fraction[..^1];
            }

            var trimmed = fraction.TrimEnd('0');

            return new Operands
            {
                N = double.Parse(intPart.Length == 0 ? "0" : intPart, CultureInfo.InvariantCulture)
                    + (fraction.Length == 0 ? 0 : double.Parse("0." + fraction, CultureInfo.InvariantCulture)),
                I = long.TryParse(intPart, out var i) ? i : long.MaxValue,
                V = fraction.Length,
                W = trimmed.Length,
                F = fraction.Length == 0 ? 0 : long.TryParse(fraction, out var f) ? f : 0,
                T = trimmed.Length == 0 ? 0 : long.TryParse(trimmed, out var t) ? t : 0,
                E = exponent,
                C = exponent,
            };
        }
    }
}
