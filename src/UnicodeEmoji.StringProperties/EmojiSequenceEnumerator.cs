using UnicodeEmoji.StringProperties.Internal;

namespace UnicodeEmoji.StringProperties;

/// <summary>
/// A forward-only, allocation-free enumerator over the emoji sequences in a span of text.
/// </summary>
/// <remarks>
/// Matching is greedy (leftmost-longest): at each position the longest emoji sequence is taken, then
/// scanning resumes after it. Characters that do not begin an emoji are skipped one scalar at a time.
/// Because this is a <see langword="ref"/> struct it can be used directly in a <c>foreach</c> loop but
/// cannot be boxed, captured in a lambda, or stored on the heap.
/// </remarks>
public ref struct EmojiSequenceEnumerator
{
    private readonly ReadOnlySpan<char> _text;
    private readonly EmojiSequenceProperties _filter;
    private int _index;
    private EmojiMatch _current;

    internal EmojiSequenceEnumerator(ReadOnlySpan<char> text, EmojiSequenceProperties filter)
    {
        _text = text;
        _filter = filter;
        _index = 0;
        _current = default;
    }

    /// <summary>Returns this enumerator, enabling use in a <c>foreach</c> statement.</summary>
    public readonly EmojiSequenceEnumerator GetEnumerator() => this;

    /// <summary>The match produced by the most recent successful <see cref="MoveNext"/>.</summary>
    public readonly EmojiMatch Current => _current;

    /// <summary>Advances to the next emoji sequence that satisfies the filter.</summary>
    /// <returns><see langword="true"/> if another match was found; otherwise <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        while (_index < _text.Length)
        {
            if (EmojiTrie.TryMatchLongest(_text, _index, out int length, out EmojiSequenceProperties props))
            {
                int start = _index;
                _index += length;
                if ((props & _filter) != 0)
                {
                    _current = new EmojiMatch(start, length, props);
                    return true;
                }

                // An emoji matched but was filtered out; skip the whole sequence so its sub-parts
                // are never reported.
                continue;
            }

            _index += EmojiTrie.DecodeScalar(_text, _index, out _);
        }

        return false;
    }
}
