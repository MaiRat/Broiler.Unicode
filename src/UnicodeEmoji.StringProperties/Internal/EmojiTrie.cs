using System.Runtime.CompilerServices;
using System.Text;

namespace UnicodeEmoji.StringProperties.Internal;

/// <summary>
/// Read-only matcher over the generated, flattened emoji trie (<see cref="EmojiTrieData"/>).
/// </summary>
/// <remarks>
/// The trie is keyed on Unicode <em>scalar values</em> (code points), not UTF-16 code units, so
/// surrogate pairs are decoded before walking. Node 0 is the root. The edges of node <c>i</c> occupy
/// the slice <c>[ChildOffsets[i], ChildOffsets[i + 1])</c> of the parallel <c>ChildKeys</c> /
/// <c>ChildNodes</c> arrays, sorted ascending by key so each step is a binary search.
/// </remarks>
internal static class EmojiTrie
{
    /// <summary>
    /// Decodes the Unicode scalar value at <paramref name="index"/> in <paramref name="text"/>.
    /// </summary>
    /// <returns>The number of UTF-16 code units consumed (1 or 2).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int DecodeScalar(ReadOnlySpan<char> text, int index, out int scalar)
    {
        char c = text[index];
        if (char.IsHighSurrogate(c) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            scalar = char.ConvertToUtf32(c, text[index + 1]);
            return 2;
        }

        // Lone surrogates and BMP characters are treated as a single scalar; lone surrogates will
        // simply fail to match any edge, which is the desired behaviour.
        scalar = c;
        return 1;
    }

    /// <summary>Finds the child node reached from <paramref name="node"/> via edge <paramref name="scalar"/>.</summary>
    /// <returns>The child node index, or <c>-1</c> if there is no such edge.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Step(int node, int scalar)
    {
        int[] offsets = EmojiTrieData.ChildOffsets;
        int[] keys = EmojiTrieData.ChildKeys;

        int lo = offsets[node];
        int hi = offsets[node + 1] - 1;
        while (lo <= hi)
        {
            int mid = (int)(((uint)lo + (uint)hi) >> 1);
            int key = keys[mid];
            if (key == scalar)
            {
                return EmojiTrieData.ChildNodes[mid];
            }

            if (key < scalar)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return -1;
    }

    /// <summary>
    /// Tests whether the entire span is exactly one emoji sequence.
    /// </summary>
    internal static bool TryMatchExact(ReadOnlySpan<char> text, out EmojiSequenceProperties properties)
    {
        properties = EmojiSequenceProperties.None;
        if (text.IsEmpty)
        {
            return false;
        }

        int node = 0;
        int i = 0;
        while (i < text.Length)
        {
            i += DecodeScalar(text, i, out int scalar);
            node = Step(node, scalar);
            if (node < 0)
            {
                return false;
            }
        }

        properties = (EmojiSequenceProperties)EmojiTrieData.Props[node];
        return properties != EmojiSequenceProperties.None;
    }

    /// <summary>
    /// Collects every emoji sequence stored in the trie whose properties intersect
    /// <paramref name="filter"/>, as UTF-16 strings. The trie is a tree (edges == nodes - 1),
    /// so a depth-first walk visits each sequence exactly once. Order is unspecified.
    /// </summary>
    internal static List<string> CollectSequences(EmojiSequenceProperties filter)
    {
        var result = new List<string>();
        var scalars = new List<int>(MaxDepth);
        Walk(0, scalars, filter, result);
        return result;

        static void Walk(int node, List<int> scalars, EmojiSequenceProperties filter, List<string> result)
        {
            var props = (EmojiSequenceProperties)EmojiTrieData.Props[node];
            if (scalars.Count > 0 && (props & filter) != 0)
            {
                var sb = new StringBuilder(scalars.Count);
                foreach (int scalar in scalars)
                {
                    sb.Append(char.ConvertFromUtf32(scalar));
                }

                result.Add(sb.ToString());
            }

            int start = EmojiTrieData.ChildOffsets[node];
            int end = EmojiTrieData.ChildOffsets[node + 1];
            for (int edge = start; edge < end; edge++)
            {
                scalars.Add(EmojiTrieData.ChildKeys[edge]);
                Walk(EmojiTrieData.ChildNodes[edge], scalars, filter, result);
                scalars.RemoveAt(scalars.Count - 1);
            }
        }
    }

    // A sequence is at most MaxSequenceLength scalars; used to size the DFS path buffer.
    private const int MaxDepth = EmojiTrieData.MaxSequenceLength;

    /// <summary>
    /// Finds the longest emoji sequence that starts at <paramref name="start"/>.
    /// </summary>
    /// <param name="text">The text being scanned.</param>
    /// <param name="start">The starting UTF-16 index.</param>
    /// <param name="length">On success, the length of the match in UTF-16 code units.</param>
    /// <param name="properties">On success, the properties of the matched sequence.</param>
    /// <returns><see langword="true"/> if an emoji sequence begins at <paramref name="start"/>.</returns>
    internal static bool TryMatchLongest(
        ReadOnlySpan<char> text,
        int start,
        out int length,
        out EmojiSequenceProperties properties)
    {
        int node = 0;
        int i = start;
        int bestEnd = -1;
        EmojiSequenceProperties bestProps = EmojiSequenceProperties.None;

        while (i < text.Length)
        {
            i += DecodeScalar(text, i, out int scalar);
            node = Step(node, scalar);
            if (node < 0)
            {
                break;
            }

            var props = (EmojiSequenceProperties)EmojiTrieData.Props[node];
            if (props != EmojiSequenceProperties.None)
            {
                // Record the longest terminal seen so far (greedy / leftmost-longest).
                bestEnd = i;
                bestProps = props;
            }
        }

        if (bestEnd < 0)
        {
            length = 0;
            properties = EmojiSequenceProperties.None;
            return false;
        }

        length = bestEnd - start;
        properties = bestProps;
        return true;
    }
}
