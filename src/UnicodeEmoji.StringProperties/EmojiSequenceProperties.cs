using System;

namespace UnicodeEmoji.StringProperties;

/// <summary>
/// Unicode emoji string properties (UTS #51) that a sequence of Unicode scalar values can satisfy.
/// </summary>
/// <remarks>
/// These flags mirror the <c>type_field</c> values in the official
/// <c>emoji-sequences.txt</c> and <c>emoji-zwj-sequences.txt</c> data files.
/// A single sequence is classified by exactly one underlying type field, but the flags are
/// modelled as a <see cref="FlagsAttribute"/> enumeration so callers can test membership cheaply.
/// <para>
/// <see cref="RgiEmoji"/> is the union of all the individual sequence properties and corresponds to
/// the <c>RGI_Emoji</c> set defined in UTS #51. It is a property of a <em>string</em> (a sequence of
/// scalar values), not of a single code point.
/// </para>
/// </remarks>
[Flags]
public enum EmojiSequenceProperties : byte
{
    /// <summary>The sequence is not a recognized RGI emoji.</summary>
    None = 0,

    /// <summary>
    /// <c>Basic_Emoji</c>: a single emoji code point (optionally followed by U+FE0F), for example
    /// <c>😀</c> (U+1F600) or <c>❤️</c> (U+2764 U+FE0F).
    /// </summary>
    BasicEmoji = 1 << 0,

    /// <summary>
    /// <c>Emoji_Keycap_Sequence</c>: a keycap such as <c>1️⃣</c> (U+0031 U+FE0F U+20E3).
    /// </summary>
    EmojiKeycapSequence = 1 << 1,

    /// <summary>
    /// <c>RGI_Emoji_Flag_Sequence</c>: a pair of regional indicator symbols, for example
    /// <c>🇩🇪</c> (U+1F1E9 U+1F1EA).
    /// </summary>
    RgiEmojiFlagSequence = 1 << 2,

    /// <summary>
    /// <c>RGI_Emoji_Tag_Sequence</c>: a black flag followed by tag characters, for example the
    /// England flag <c>🏴󠁧󠁢󠁥󠁮󠁧󠁿</c>.
    /// </summary>
    RgiEmojiTagSequence = 1 << 3,

    /// <summary>
    /// <c>RGI_Emoji_Modifier_Sequence</c>: a base emoji followed by a skin-tone modifier, for example
    /// <c>👋🏽</c> (U+1F44B U+1F3FD).
    /// </summary>
    RgiEmojiModifierSequence = 1 << 4,

    /// <summary>
    /// <c>RGI_Emoji_ZWJ_Sequence</c>: an emoji joined with U+200D (zero width joiner), for example the
    /// family <c>👨‍👩‍👧‍👦</c> or <c>👩🏽‍💻</c>.
    /// </summary>
    RgiEmojiZwjSequence = 1 << 5,

    /// <summary>
    /// <c>RGI_Emoji</c>: the union of all the individual RGI sequence properties. A string has this
    /// property if it exactly matches any recommended-for-general-interchange emoji.
    /// </summary>
    RgiEmoji =
        BasicEmoji |
        EmojiKeycapSequence |
        RgiEmojiFlagSequence |
        RgiEmojiTagSequence |
        RgiEmojiModifierSequence |
        RgiEmojiZwjSequence,
}
