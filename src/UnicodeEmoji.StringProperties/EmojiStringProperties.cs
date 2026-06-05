using System;
using System.Collections.Generic;
using UnicodeEmoji.StringProperties.Internal;

namespace UnicodeEmoji.StringProperties;

/// <summary>
/// Tests whether a string of Unicode scalar values has an emoji string property defined by UTS #51,
/// and enumerates the RGI emoji contained in arbitrary text.
/// </summary>
/// <remarks>
/// <para>
/// Matching is performed against a compact, generated trie built from the official Unicode emoji data
/// files (<c>emoji-sequences.txt</c> and <c>emoji-zwj-sequences.txt</c>). No data is downloaded or read
/// from disk at runtime, and there is no dependency on ICU or any native component.
/// </para>
/// <para>
/// The exact-match methods (<see cref="IsRgiEmoji(ReadOnlySpan{char})"/> and friends) require the
/// <em>entire</em> input to be a single fully-qualified emoji sequence. Use
/// <see cref="EnumerateRgiEmoji(ReadOnlySpan{char})"/> to find emoji embedded in longer text.
/// </para>
/// </remarks>
public static class EmojiStringProperties
{
    /// <summary>The Unicode emoji data version the lookup tables were generated from (for example <c>"16.0"</c>).</summary>
    public static string UnicodeEmojiVersion => EmojiTrieData.UnicodeEmojiVersion;

    /// <summary>
    /// Gets every emoji property satisfied by the input, treated as a single sequence.
    /// </summary>
    /// <param name="value">The text to classify.</param>
    /// <returns>
    /// The combined <see cref="EmojiSequenceProperties"/> of the sequence, or
    /// <see cref="EmojiSequenceProperties.None"/> if the input is not exactly an RGI emoji.
    /// </returns>
    public static EmojiSequenceProperties GetProperties(ReadOnlySpan<char> value)
    {
        EmojiTrie.TryMatchExact(value, out EmojiSequenceProperties properties);
        return properties;
    }

    /// <inheritdoc cref="GetProperties(ReadOnlySpan{char})"/>
    /// <param name="value">The text to classify. Must not be <see langword="null"/>.</param>
    public static EmojiSequenceProperties GetProperties(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return GetProperties(value.AsSpan());
    }

    /// <summary>
    /// Determines whether the entire input is a single recommended-for-general-interchange (RGI) emoji.
    /// </summary>
    /// <param name="value">The text to test.</param>
    /// <returns><see langword="true"/> if the input has the <c>RGI_Emoji</c> property; otherwise <see langword="false"/>.</returns>
    public static bool IsRgiEmoji(ReadOnlySpan<char> value) => GetProperties(value) != EmojiSequenceProperties.None;

    /// <inheritdoc cref="IsRgiEmoji(ReadOnlySpan{char})"/>
    /// <param name="value">The text to test. Must not be <see langword="null"/>.</param>
    public static bool IsRgiEmoji(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return IsRgiEmoji(value.AsSpan());
    }

    /// <summary>
    /// Determines whether the entire input is a <c>Basic_Emoji</c> (a single emoji code point,
    /// optionally followed by U+FE0F).
    /// </summary>
    /// <param name="value">The text to test.</param>
    /// <returns><see langword="true"/> if the input has the <c>Basic_Emoji</c> property; otherwise <see langword="false"/>.</returns>
    public static bool IsBasicEmoji(ReadOnlySpan<char> value) =>
        (GetProperties(value) & EmojiSequenceProperties.BasicEmoji) != 0;

    /// <inheritdoc cref="IsBasicEmoji(ReadOnlySpan{char})"/>
    /// <param name="value">The text to test. Must not be <see langword="null"/>.</param>
    public static bool IsBasicEmoji(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return IsBasicEmoji(value.AsSpan());
    }

    /// <summary>
    /// Determines whether the entire input is an <c>RGI_Emoji_ZWJ_Sequence</c> (emoji joined with U+200D).
    /// </summary>
    /// <param name="value">The text to test.</param>
    /// <returns><see langword="true"/> if the input has the <c>RGI_Emoji_ZWJ_Sequence</c> property; otherwise <see langword="false"/>.</returns>
    public static bool IsEmojiZwjSequence(ReadOnlySpan<char> value) =>
        (GetProperties(value) & EmojiSequenceProperties.RgiEmojiZwjSequence) != 0;

    /// <inheritdoc cref="IsEmojiZwjSequence(ReadOnlySpan{char})"/>
    /// <param name="value">The text to test. Must not be <see langword="null"/>.</param>
    public static bool IsEmojiZwjSequence(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return IsEmojiZwjSequence(value.AsSpan());
    }

    /// <summary>
    /// Determines whether the entire input satisfies <em>all</em> of the requested properties.
    /// </summary>
    /// <param name="value">The text to test.</param>
    /// <param name="properties">The property mask the input must satisfy. Bits outside the mask are ignored.</param>
    /// <returns><see langword="true"/> if every requested property bit is present; otherwise <see langword="false"/>.</returns>
    public static bool HasProperties(ReadOnlySpan<char> value, EmojiSequenceProperties properties) =>
        (GetProperties(value) & properties) == properties;

    /// <summary>
    /// Returns every emoji sequence in the bundled data whose properties intersect
    /// <paramref name="filter"/>, as UTF-16 strings (for example every <c>Emoji_Keycap_Sequence</c>,
    /// or — with <see cref="EmojiSequenceProperties.RgiEmoji"/> — the full RGI set).
    /// </summary>
    /// <param name="filter">A mask selecting which sequences to return. A sequence is included
    /// when it satisfies any set bit.</param>
    /// <returns>The matching sequences. Order is unspecified; callers that need a stable or
    /// length-sorted order should sort the result themselves.</returns>
    /// <remarks>
    /// Intended for building an enumeration of a string property (for example to expand a
    /// <c>\p{RGI_Emoji}</c> regular-expression escape into an alternation). The result is
    /// materialized fresh on each call.
    /// </remarks>
    public static IReadOnlyList<string> GetSequences(EmojiSequenceProperties filter) =>
        EmojiTrie.CollectSequences(filter);

    /// <summary>
    /// Enumerates the RGI emoji contained in <paramref name="text"/> using greedy, allocation-free,
    /// leftmost-longest matching.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>An enumerator yielding an <see cref="EmojiMatch"/> for each emoji found.</returns>
    public static EmojiSequenceEnumerator EnumerateRgiEmoji(ReadOnlySpan<char> text) =>
        new(text, EmojiSequenceProperties.RgiEmoji);

    /// <summary>
    /// Enumerates the emoji in <paramref name="text"/> that satisfy any of the requested
    /// <paramref name="filter"/> properties.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <param name="filter">A mask of properties to report. A match is yielded when it satisfies any set bit.</param>
    /// <returns>An enumerator yielding an <see cref="EmojiMatch"/> for each matching emoji.</returns>
    public static EmojiSequenceEnumerator Enumerate(ReadOnlySpan<char> text, EmojiSequenceProperties filter) =>
        new(text, filter);
}
