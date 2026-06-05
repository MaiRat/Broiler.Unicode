using System.Collections.Generic;
using Xunit;
using static UnicodeEmoji.StringProperties.Tests.TestText;

namespace UnicodeEmoji.StringProperties.Tests;

/// <summary>Tests for allocation-free enumeration of emoji embedded in longer text.</summary>
public class EnumerationTests
{
    private static List<(string Text, EmojiSequenceProperties Props)> Collect(string text)
    {
        var found = new List<(string, EmojiSequenceProperties)>();
        foreach (EmojiMatch match in EmojiStringProperties.EnumerateRgiEmoji(text))
        {
            // The index/length must round-trip to a valid RGI emoji slice (no string was allocated by the API).
            Assert.True(EmojiStringProperties.IsRgiEmoji(text.AsSpan(match.Index, match.Length)));
            Assert.NotEqual(EmojiSequenceProperties.None, match.Properties);
            found.Add((text.Substring(match.Index, match.Length), match.Properties));
        }

        return found;
    }

    [Fact]
    public void Enumerate_FindsEmojiInMixedText_InOrder()
    {
        string wave = FromScalars(0x1F44B);
        string family = FromScalars(0x1F468, 0x200D, 0x1F469, 0x200D, 0x1F467, 0x200D, 0x1F466);
        string keycap = FromScalars(0x0031, 0xFE0F, 0x20E3);
        string flag = FromScalars(0x1F1E9, 0x1F1EA);
        string text = $"Hi {wave} fam {family}! {keycap} {flag} end";

        List<(string Text, EmojiSequenceProperties Props)> found = Collect(text);

        Assert.Equal(
            new[] { wave, family, keycap, flag },
            found.ConvertAll(f => f.Text));
        Assert.Equal(EmojiSequenceProperties.RgiEmojiZwjSequence, found[1].Props);
        Assert.Equal(EmojiSequenceProperties.EmojiKeycapSequence, found[2].Props);
        Assert.Equal(EmojiSequenceProperties.RgiEmojiFlagSequence, found[3].Props);
    }

    [Fact]
    public void Enumerate_AdjacentFlags_AreSplit()
    {
        string de = FromScalars(0x1F1E9, 0x1F1EA);
        string fr = FromScalars(0x1F1EB, 0x1F1F7);
        List<(string Text, EmojiSequenceProperties Props)> found = Collect(de + fr);

        Assert.Equal(new[] { de, fr }, found.ConvertAll(f => f.Text));
    }

    [Fact]
    public void Enumerate_PrefersLongestMatch()
    {
        // The full family must win over its shorter prefixes (e.g. man+woman couple).
        string family = FromScalars(0x1F468, 0x200D, 0x1F469, 0x200D, 0x1F467, 0x200D, 0x1F466);
        List<(string Text, EmojiSequenceProperties Props)> found = Collect(family);

        Assert.Single(found);
        Assert.Equal(family, found[0].Text);
    }

    [Fact]
    public void Enumerate_FilterRestrictsResults()
    {
        string grin = FromScalars(0x1F600);
        string flag = FromScalars(0x1F1E9, 0x1F1EA);
        string text = grin + flag;

        var basics = new List<string>();
        foreach (EmojiMatch match in EmojiStringProperties.Enumerate(text, EmojiSequenceProperties.BasicEmoji))
        {
            basics.Add(text.Substring(match.Index, match.Length));
        }

        Assert.Equal(new[] { grin }, basics);
    }

    [Fact]
    public void Enumerate_TextWithoutEmoji_YieldsNothing()
    {
        Assert.Empty(Collect("just some plain ASCII text, no emoji here."));
    }
}
