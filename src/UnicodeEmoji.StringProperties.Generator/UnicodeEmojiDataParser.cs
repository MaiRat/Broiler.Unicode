using System.Globalization;

namespace UnicodeEmoji.StringProperties.Generator;

/// <summary>
/// Parses the official Unicode emoji data files into normalized sequences of scalar values.
/// </summary>
/// <remarks>
/// The parser understands the shared <c>code_points ; type_field ; description # comment</c> grammar of
/// <c>emoji-sequences.txt</c> and <c>emoji-zwj-sequences.txt</c>, including <c>start..end</c> code point
/// ranges (used by <c>Basic_Emoji</c>), as well as the <c>code_points ; status # name</c> grammar of
/// <c>emoji-test.txt</c>.
/// </remarks>
public static class UnicodeEmojiDataParser
{
    /// <summary>
    /// Parses <c>emoji-sequences.txt</c> and <c>emoji-zwj-sequences.txt</c> from a data directory.
    /// </summary>
    /// <param name="dataDirectory">A directory containing the emoji data files for a single version.</param>
    /// <returns>All parsed sequences, in file order.</returns>
    public static IReadOnlyList<EmojiSequence> ParseSequences(string dataDirectory)
    {
        var result = new List<EmojiSequence>();
        result.AddRange(ParseSequenceFile(Path.Combine(dataDirectory, "emoji-sequences.txt")));
        result.AddRange(ParseSequenceFile(Path.Combine(dataDirectory, "emoji-zwj-sequences.txt")));
        return result;
    }

    /// <summary>Parses a single sequence file (either <c>emoji-sequences.txt</c> or <c>emoji-zwj-sequences.txt</c>).</summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The parsed sequences.</returns>
    public static IEnumerable<EmojiSequence> ParseSequenceFile(string path)
    {
        foreach (string rawLine in File.ReadLines(path))
        {
            string line = StripComment(rawLine);
            if (line.Length == 0)
            {
                continue;
            }

            string[] fields = line.Split(';');
            if (fields.Length < 2)
            {
                continue;
            }

            string codePoints = fields[0].Trim();
            EmojiPropertyFlags property = MapTypeField(fields[1].Trim());
            if (property == EmojiPropertyFlags.None)
            {
                continue;
            }

            string name = fields.Length >= 3 ? fields[2].Trim() : string.Empty;

            int rangeMarker = codePoints.IndexOf("..", StringComparison.Ordinal);
            if (rangeMarker >= 0)
            {
                // A range such as "231A..231B" expands to one single-scalar Basic_Emoji per code point.
                int start = ParseScalar(codePoints.AsSpan(0, rangeMarker));
                int end = ParseScalar(codePoints.AsSpan(rangeMarker + 2));
                for (int cp = start; cp <= end; cp++)
                {
                    yield return new EmojiSequence([cp], property, name);
                }
            }
            else
            {
                yield return new EmojiSequence(ParseScalars(codePoints), property, name);
            }
        }
    }

    /// <summary>Parses <c>emoji-test.txt</c>.</summary>
    /// <param name="path">The path to <c>emoji-test.txt</c>.</param>
    /// <returns>The parsed entries, in file order.</returns>
    public static IEnumerable<EmojiTestEntry> ParseEmojiTest(string path)
    {
        foreach (string rawLine in File.ReadLines(path))
        {
            if (rawLine.Length == 0 || rawLine[0] == '#')
            {
                continue;
            }

            int hash = rawLine.IndexOf('#');
            string data = (hash >= 0 ? rawLine[..hash] : rawLine);
            string comment = hash >= 0 ? rawLine[(hash + 1)..].Trim() : string.Empty;

            string[] fields = data.Split(';');
            if (fields.Length < 2)
            {
                continue;
            }

            int[] scalars = ParseScalars(fields[0].Trim());
            EmojiTestStatus status = fields[1].Trim() switch
            {
                "fully-qualified" => EmojiTestStatus.FullyQualified,
                "minimally-qualified" => EmojiTestStatus.MinimallyQualified,
                "unqualified" => EmojiTestStatus.Unqualified,
                "component" => EmojiTestStatus.Component,
                _ => EmojiTestStatus.Unqualified,
            };

            yield return new EmojiTestEntry(scalars, status, comment);
        }
    }

    private static EmojiPropertyFlags MapTypeField(string typeField) => typeField switch
    {
        "Basic_Emoji" => EmojiPropertyFlags.BasicEmoji,
        "Emoji_Keycap_Sequence" => EmojiPropertyFlags.EmojiKeycapSequence,
        "RGI_Emoji_Flag_Sequence" => EmojiPropertyFlags.RgiEmojiFlagSequence,
        "RGI_Emoji_Tag_Sequence" => EmojiPropertyFlags.RgiEmojiTagSequence,
        "RGI_Emoji_Modifier_Sequence" => EmojiPropertyFlags.RgiEmojiModifierSequence,
        "RGI_Emoji_ZWJ_Sequence" => EmojiPropertyFlags.RgiEmojiZwjSequence,
        _ => EmojiPropertyFlags.None,
    };

    private static string StripComment(string line)
    {
        int hash = line.IndexOf('#');
        return (hash >= 0 ? line[..hash] : line).Trim();
    }

    private static int[] ParseScalars(string codePoints)
    {
        string[] parts = codePoints.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var scalars = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            scalars[i] = ParseScalar(parts[i].AsSpan());
        }

        return scalars;
    }

    private static int ParseScalar(ReadOnlySpan<char> hex) =>
        int.Parse(hex.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
