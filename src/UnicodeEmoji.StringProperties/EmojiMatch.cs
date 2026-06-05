namespace UnicodeEmoji.StringProperties;

/// <summary>
/// Describes a single RGI emoji found while scanning text. The match references a region of the
/// source text by index and length; no substring is allocated.
/// </summary>
/// <remarks>
/// <see cref="Index"/> and <see cref="Length"/> are measured in UTF-16 code units (<see cref="char"/>),
/// so the matched text can be recovered with <c>text.Slice(match.Index, match.Length)</c>.
/// </remarks>
public readonly struct EmojiMatch
{
    /// <summary>Initializes a new <see cref="EmojiMatch"/>.</summary>
    /// <param name="index">The start index of the match, in UTF-16 code units.</param>
    /// <param name="length">The length of the match, in UTF-16 code units.</param>
    /// <param name="properties">The emoji properties satisfied by the matched sequence.</param>
    public EmojiMatch(int index, int length, EmojiSequenceProperties properties)
    {
        Index = index;
        Length = length;
        Properties = properties;
    }

    /// <summary>The start of the match within the source text, in UTF-16 code units.</summary>
    public int Index { get; }

    /// <summary>The length of the match within the source text, in UTF-16 code units.</summary>
    public int Length { get; }

    /// <summary>The exclusive end of the match within the source text (<see cref="Index"/> + <see cref="Length"/>).</summary>
    public int End => Index + Length;

    /// <summary>The emoji properties satisfied by the matched sequence.</summary>
    public EmojiSequenceProperties Properties { get; }
}
