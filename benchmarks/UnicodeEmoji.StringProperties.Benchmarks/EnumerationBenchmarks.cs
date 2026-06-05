using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace UnicodeEmoji.StringProperties.Benchmarks;

/// <summary>
/// Compares scanning a paragraph for embedded RGI emoji using the generated trie, a naive
/// HashSet-based substring probe, and a large compiled <see cref="Regex"/>.
/// </summary>
[MemoryDiagnoser]
public class EnumerationBenchmarks
{
    private string _text = string.Empty;
    private HashSet<string> _set = [];
    private int _maxLen;
    private Regex _regex = null!;

    [GlobalSetup]
    public void Setup()
    {
        _text = EmojiDataset.SampleParagraph;
        _set = new HashSet<string>(EmojiDataset.AllEmoji, StringComparer.Ordinal);
        _maxLen = EmojiDataset.MaxLengthChars;
        _regex = BuildRegex(EmojiDataset.AllEmoji);
    }

    [Benchmark(Baseline = true, Description = "Generated trie enumerate")]
    public int Trie()
    {
        int count = 0;
        foreach (EmojiMatch _ in EmojiStringProperties.EnumerateRgiEmoji(_text))
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "HashSet substring probe")]
    public int HashSetProbe()
    {
        int count = 0;
        int i = 0;
        while (i < _text.Length)
        {
            int matchedLen = 0;
            // Naive longest-match: try the longest possible window first.
            int maxWindow = Math.Min(_maxLen, _text.Length - i);
            for (int len = maxWindow; len >= 1; len--)
            {
                if (_set.Contains(_text.Substring(i, len)))
                {
                    matchedLen = len;
                    break;
                }
            }

            if (matchedLen > 0)
            {
                count++;
                i += matchedLen;
            }
            else
            {
                i++;
            }
        }

        return count;
    }

    [Benchmark(Description = "Compiled Regex matches")]
    public int RegexMatches()
    {
        int count = 0;
        foreach (Match _ in _regex.Matches(_text))
        {
            count++;
        }

        return count;
    }

    private static Regex BuildRegex(IReadOnlyList<string> emoji)
    {
        var ordered = new List<string>(emoji);
        ordered.Sort((a, b) => b.Length.CompareTo(a.Length));

        var sb = new StringBuilder("(?:");
        for (int i = 0; i < ordered.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('|');
            }

            sb.Append(Regex.Escape(ordered[i]));
        }

        sb.Append(')');
        return new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
