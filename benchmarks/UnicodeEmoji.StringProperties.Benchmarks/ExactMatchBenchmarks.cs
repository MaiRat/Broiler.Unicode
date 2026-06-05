using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace UnicodeEmoji.StringProperties.Benchmarks;

/// <summary>
/// Compares exact whole-string RGI matching across three strategies: the generated trie, a naive
/// <see cref="HashSet{T}"/> of every emoji string, and a single large compiled <see cref="Regex"/>.
/// </summary>
[MemoryDiagnoser]
public class ExactMatchBenchmarks
{
    private string[] _inputs = [];
    private HashSet<string> _set = [];
    private Regex _regex = null!;

    [GlobalSetup]
    public void Setup()
    {
        _inputs = EmojiDataset.ExactMatchInputs;
        _set = new HashSet<string>(EmojiDataset.AllEmoji, StringComparer.Ordinal);
        _regex = BuildAnchoredRegex(EmojiDataset.AllEmoji);
    }

    [Benchmark(Baseline = true, Description = "Generated trie")]
    public int Trie()
    {
        int count = 0;
        foreach (string input in _inputs)
        {
            if (EmojiStringProperties.IsRgiEmoji(input))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark(Description = "HashSet<string>")]
    public int HashSet()
    {
        int count = 0;
        foreach (string input in _inputs)
        {
            if (_set.Contains(input))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark(Description = "Compiled Regex")]
    public int RegexMatch()
    {
        int count = 0;
        foreach (string input in _inputs)
        {
            if (_regex.IsMatch(input))
            {
                count++;
            }
        }

        return count;
    }

    private static Regex BuildAnchoredRegex(IReadOnlyList<string> emoji)
    {
        // Longest-first alternation so the regex prefers complete sequences.
        var ordered = new List<string>(emoji);
        ordered.Sort((a, b) => b.Length.CompareTo(a.Length));

        var sb = new StringBuilder("^(?:");
        for (int i = 0; i < ordered.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('|');
            }

            sb.Append(Regex.Escape(ordered[i]));
        }

        sb.Append(")$");
        return new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
