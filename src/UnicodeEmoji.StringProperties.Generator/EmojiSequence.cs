namespace UnicodeEmoji.StringProperties.Generator;

/// <summary>
/// Property bits emitted into the generated trie. These values must stay in sync with the public
/// <c>UnicodeEmoji.StringProperties.EmojiSequenceProperties</c> enumeration.
/// </summary>
[Flags]
public enum EmojiPropertyFlags : byte
{
    None = 0,
    BasicEmoji = 1 << 0,
    EmojiKeycapSequence = 1 << 1,
    RgiEmojiFlagSequence = 1 << 2,
    RgiEmojiTagSequence = 1 << 3,
    RgiEmojiModifierSequence = 1 << 4,
    RgiEmojiZwjSequence = 1 << 5,
}

/// <summary>
/// A single emoji sequence parsed from a Unicode data file: a list of scalar values, the property it
/// satisfies, and its CLDR short name.
/// </summary>
/// <param name="Scalars">The Unicode scalar values (code points) that make up the sequence.</param>
/// <param name="Property">The property the sequence satisfies.</param>
/// <param name="Name">The CLDR short name (for diagnostics; not used by the runtime).</param>
public sealed record EmojiSequence(int[] Scalars, EmojiPropertyFlags Property, string Name);

/// <summary>The qualification status of an entry in <c>emoji-test.txt</c>.</summary>
public enum EmojiTestStatus
{
    Component,
    FullyQualified,
    MinimallyQualified,
    Unqualified,
}

/// <summary>A single entry parsed from <c>emoji-test.txt</c>.</summary>
/// <param name="Scalars">The Unicode scalar values that make up the entry.</param>
/// <param name="Status">The qualification status.</param>
/// <param name="Name">The display name from the trailing comment.</param>
public sealed record EmojiTestEntry(int[] Scalars, EmojiTestStatus Status, string Name);
