namespace UnicodeEmoji.StringProperties.Generator;

/// <summary>Summary of a generation run.</summary>
/// <param name="SequenceCount">The number of distinct sequences encoded.</param>
/// <param name="NodeCount">The number of trie nodes.</param>
/// <param name="EdgeCount">The number of trie edges.</param>
/// <param name="MaxSequenceLength">The longest sequence length in scalar values.</param>
/// <param name="OutputPath">The path of the generated source file.</param>
public sealed record GenerationResult(int SequenceCount, int NodeCount, int EdgeCount, int MaxSequenceLength, string OutputPath);

/// <summary>
/// End-to-end facade: parse the Unicode data files for a version, build the trie, and write the
/// generated <c>EmojiTrieData</c> source file.
/// </summary>
public static class EmojiCodeGenerator
{
    /// <summary>Runs the full parse → build → emit pipeline.</summary>
    /// <param name="dataDirectory">Directory containing the emoji data files for the version.</param>
    /// <param name="unicodeEmojiVersion">The Unicode emoji version string, for example <c>"16.0"</c>.</param>
    /// <param name="outputPath">The path of the generated source file to write.</param>
    /// <returns>A summary of what was generated.</returns>
    public static GenerationResult Generate(string dataDirectory, string unicodeEmojiVersion, string outputPath)
    {
        IReadOnlyList<EmojiSequence> sequences = UnicodeEmojiDataParser.ParseSequences(dataDirectory);
        if (sequences.Count == 0)
        {
            throw new InvalidOperationException($"No emoji sequences were parsed from '{dataDirectory}'.");
        }

        var builder = new EmojiTrieBuilder();
        builder.AddRange(sequences);
        FlatTrie trie = builder.Build();

        int maxLength = 0;
        foreach (EmojiSequence sequence in sequences)
        {
            if (sequence.Scalars.Length > maxLength)
            {
                maxLength = sequence.Scalars.Length;
            }
        }

        string source = EmojiTrieSourceWriter.Write(trie, unicodeEmojiVersion, sequences.Count, maxLength);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Normalize to LF so the committed generated file is stable across platforms.
        File.WriteAllText(outputPath, source.Replace("\r\n", "\n"));
        return new GenerationResult(sequences.Count, trie.NodeCount, trie.EdgeCount, maxLength, outputPath);
    }
}
