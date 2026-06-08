using System.Text.Json;

namespace UnicodeCldr.LocaleData.Generator;

/// <summary>The ordered non-"other" plural rules for one (locale, type).</summary>
/// <param name="Locale">The CLDR locale id, normalized to lowercase with '-' separators.</param>
/// <param name="Type">ECMA-402 plural type: <c>"cardinal"</c> or <c>"ordinal"</c>.</param>
/// <param name="Rules">(category, condition) pairs in CLDR order; "other" is the implicit fallback.</param>
public sealed record LocalePluralRules(string Locale, string Type, IReadOnlyList<(string Category, string Condition)> Rules);

/// <summary>
/// Parses CLDR <c>plurals.json</c> / <c>ordinals.json</c> (a single file each, all
/// locales) into ordered plural-rule conditions, stripping the <c>@integer</c> /
/// <c>@decimal</c> sample lists.
/// </summary>
public static class CldrPluralRulesParser
{
    /// <summary>Parses the cardinal and ordinal supplemental files under <paramref name="supplementalDirectory"/>.</summary>
    public static IReadOnlyList<LocalePluralRules> ParseAll(string supplementalDirectory)
    {
        var result = new List<LocalePluralRules>();
        ParseFile(Path.Combine(supplementalDirectory, "plurals.json"), "plurals-type-cardinal", "cardinal", result);
        ParseFile(Path.Combine(supplementalDirectory, "ordinals.json"), "plurals-type-ordinal", "ordinal", result);
        return result;
    }

    private static void ParseFile(string path, string groupKey, string ecmaType, List<LocalePluralRules> result)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("supplemental", out var supplemental)
            || !supplemental.TryGetProperty(groupKey, out var group))
        {
            return;
        }

        foreach (var locale in group.EnumerateObject())
        {
            var rules = new List<(string, string)>();
            foreach (var rule in locale.Value.EnumerateObject())
            {
                // rule name is "pluralRule-count-<category>".
                var category = rule.Name[(rule.Name.LastIndexOf('-') + 1)..];
                if (category == "other")
                {
                    continue;
                }

                var condition = StripSamples(rule.Value.GetString());
                if (condition.Length == 0)
                {
                    continue;
                }

                rules.Add((category, condition));
            }

            if (rules.Count > 0)
            {
                result.Add(new LocalePluralRules(locale.Name.ToLowerInvariant().Replace('_', '-'), ecmaType, rules));
            }
        }
    }

    // The rule text is "<condition> @integer ... @decimal ...". Keep the condition.
    private static string StripSamples(string? rule)
    {
        if (string.IsNullOrEmpty(rule))
        {
            return string.Empty;
        }

        var at = rule.IndexOf('@');
        return (at < 0 ? rule : rule[..at]).Trim();
    }
}
