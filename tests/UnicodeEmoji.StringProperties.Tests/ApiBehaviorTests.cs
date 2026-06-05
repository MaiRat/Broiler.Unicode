using System;
using Xunit;
using static UnicodeEmoji.StringProperties.Tests.TestText;

namespace UnicodeEmoji.StringProperties.Tests;

/// <summary>Behavioural tests for the public API surface and its exactness guarantees.</summary>
public class ApiBehaviorTests
{
    [Fact]
    public void EmptyAndPlainText_AreNotEmoji()
    {
        Assert.False(EmojiStringProperties.IsRgiEmoji(string.Empty));
        Assert.False(EmojiStringProperties.IsRgiEmoji("a"));
        Assert.False(EmojiStringProperties.IsRgiEmoji("hello world"));
        Assert.Equal(EmojiSequenceProperties.None, EmojiStringProperties.GetProperties("x"));
    }

    [Fact]
    public void ExactMatch_RejectsPartialSequences()
    {
        // A red heart requires the U+FE0F presentation selector; the bare U+2764 is not RGI.
        Assert.False(EmojiStringProperties.IsRgiEmoji(FromScalars(0x2764)));

        // A dangling ZWJ prefix of the family sequence is not itself terminal.
        Assert.False(EmojiStringProperties.IsRgiEmoji(FromScalars(0x1F468, 0x200D)));
    }

    [Fact]
    public void ExactMatch_RejectsTwoConcatenatedEmoji()
    {
        // Each is RGI on its own, but the concatenation is not a single emoji.
        Assert.False(EmojiStringProperties.IsRgiEmoji(FromScalars(0x1F600, 0x1F600)));
    }

    [Fact]
    public void SpanAndStringOverloads_Agree()
    {
        string value = FromScalars(0x1F1E9, 0x1F1EA);
        Assert.Equal(
            EmojiStringProperties.IsRgiEmoji(value),
            EmojiStringProperties.IsRgiEmoji(value.AsSpan()));
        Assert.Equal(
            EmojiStringProperties.GetProperties(value),
            EmojiStringProperties.GetProperties(value.AsSpan()));
    }

    [Fact]
    public void NullString_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => EmojiStringProperties.IsRgiEmoji((string)null!));
        Assert.Throws<ArgumentNullException>(() => EmojiStringProperties.GetProperties((string)null!));
    }

    [Fact]
    public void HasProperties_RequiresAllRequestedBits()
    {
        string family = FromScalars(0x1F468, 0x200D, 0x1F466);
        Assert.True(EmojiStringProperties.HasProperties(family, EmojiSequenceProperties.RgiEmojiZwjSequence));
        Assert.False(EmojiStringProperties.HasProperties(family, EmojiSequenceProperties.BasicEmoji));
    }

    [Fact]
    public void Version_IsExposed()
    {
        Assert.False(string.IsNullOrWhiteSpace(EmojiStringProperties.UnicodeEmojiVersion));
    }
}
