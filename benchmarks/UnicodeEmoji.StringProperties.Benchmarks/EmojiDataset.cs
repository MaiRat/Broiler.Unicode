using System.Text;
using UnicodeEmoji.StringProperties.Generator;

namespace UnicodeEmoji.StringProperties.Benchmarks;

/// <summary>
/// Loads the committed Unicode emoji data and prepares the inputs shared by all benchmarks: the full
/// set of emoji strings, a mixed sample to exact-match, and a paragraph to scan.
/// </summary>
internal static class EmojiDataset
{
    public static IReadOnlyList<string> AllEmoji { get; } = LoadAllEmoji();

    /// <summary>A representative mix of emoji and non-emoji strings for exact-match benchmarks.</summary>
    public static string[] ExactMatchInputs { get; } = BuildExactMatchInputs();

    /// <summary>A paragraph of text with emoji sprinkled throughout, for enumeration benchmarks.</summary>
    public static string SampleParagraph { get; } = BuildParagraph();

    public static int MaxLengthChars { get; } = ComputeMaxLengthChars();

    private static IReadOnlyList<string> LoadAllEmoji()
    {
        string root = RepoLayout.FindRepoRoot(AppContext.BaseDirectory);
        string dataDir = RepoLayout.DataDirectory(root, "16.0");
        var list = new List<string>();
        foreach (EmojiSequence sequence in UnicodeEmojiDataParser.ParseSequences(dataDir))
        {
            list.Add(ToStringValue(sequence.Scalars));
        }

        return list;
    }

    private static string[] BuildExactMatchInputs()
    {
        // Half real emoji (sampled across the set), half near-miss / plain strings.
        var inputs = new List<string>();
        IReadOnlyList<string> all = AllEmoji;
        for (int i = 0; i < all.Count; i += 8)
        {
            inputs.Add(all[i]);
        }

        inputs.Add("hello");
        inputs.Add("a");
        inputs.Add(ToStringValue([0x2764])); // bare heart, missing FE0F (near miss)
        inputs.Add(ToStringValue([0x1F468, 0x200D])); // dangling ZWJ prefix
        inputs.Add("not an emoji at all");
        return inputs.ToArray();
    }

    private static string BuildParagraph()
    {
        var sb = new StringBuilder();
        IReadOnlyList<string> all = AllEmoji;
        string[] words = ["the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog"];
        for (int i = 0; i < 200; i++)
        {
            sb.Append(words[i % words.Length]).Append(' ');
            if (i % 3 == 0)
            {
                sb.Append(all[(i * 7) % all.Count]).Append(' ');
            }
        }

        return sb.ToString();
    }

    private static int ComputeMaxLengthChars()
    {
        int max = 0;
        foreach (string emoji in AllEmoji)
        {
            if (emoji.Length > max)
            {
                max = emoji.Length;
            }
        }

        return max;
    }

    private static string ToStringValue(int[] scalars)
    {
        var sb = new StringBuilder(scalars.Length * 2);
        foreach (int scalar in scalars)
        {
            sb.Append(char.ConvertFromUtf32(scalar));
        }

        return sb.ToString();
    }
}
