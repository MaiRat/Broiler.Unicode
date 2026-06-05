using Xunit;
using static UnicodeEmoji.StringProperties.Tests.TestText;

namespace UnicodeEmoji.StringProperties.Tests;

/// <summary>
/// Snapshot / golden tests for known-tricky emoji. The expected property for each case is pinned so a
/// regression in parsing or matching is caught immediately.
/// </summary>
public class GoldenTests
{
    public static TheoryData<string, string, EmojiSequenceProperties> Cases() => new()
    {
        // name, value (built from scalar values), expected exact properties
        { "grinning face 😀", FromScalars(0x1F600), EmojiSequenceProperties.BasicEmoji },
        { "red heart ❤️", FromScalars(0x2764, 0xFE0F), EmojiSequenceProperties.BasicEmoji },
        { "heart suit ♥️", FromScalars(0x2665, 0xFE0F), EmojiSequenceProperties.BasicEmoji },
        { "keycap 1️⃣", FromScalars(0x0031, 0xFE0F, 0x20E3), EmojiSequenceProperties.EmojiKeycapSequence },
        { "flag DE 🇩🇪", FromScalars(0x1F1E9, 0x1F1EA), EmojiSequenceProperties.RgiEmojiFlagSequence },
        { "black flag 🏴", FromScalars(0x1F3F4), EmojiSequenceProperties.BasicEmoji },
        {
            "England tag flag 🏴󠁧󠁢󠁥󠁮󠁧󠁿",
            FromScalars(0x1F3F4, 0xE0067, 0xE0062, 0xE0065, 0xE006E, 0xE0067, 0xE007F),
            EmojiSequenceProperties.RgiEmojiTagSequence
        },
        {
            "family 👨‍👩‍👧‍👦",
            FromScalars(0x1F468, 0x200D, 0x1F469, 0x200D, 0x1F467, 0x200D, 0x1F466),
            EmojiSequenceProperties.RgiEmojiZwjSequence
        },
        {
            "woman technologist, medium skin 👩🏽‍💻",
            FromScalars(0x1F469, 0x1F3FD, 0x200D, 0x1F4BB),
            EmojiSequenceProperties.RgiEmojiZwjSequence
        },
        {
            "people holding hands 🧑‍🤝‍🧑",
            FromScalars(0x1F9D1, 0x200D, 0x1F91D, 0x200D, 0x1F9D1),
            EmojiSequenceProperties.RgiEmojiZwjSequence
        },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void GoldenEmoji_HasExpectedProperties(string name, string value, EmojiSequenceProperties expected)
    {
        Assert.True(EmojiStringProperties.IsRgiEmoji(value), $"{name} should be RGI");
        Assert.Equal(expected, EmojiStringProperties.GetProperties(value));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void GoldenEmoji_SpecificPredicates(string name, string value, EmojiSequenceProperties expected)
    {
        Assert.Equal(
            expected.HasFlag(EmojiSequenceProperties.BasicEmoji),
            EmojiStringProperties.IsBasicEmoji(value));
        Assert.Equal(
            expected.HasFlag(EmojiSequenceProperties.RgiEmojiZwjSequence),
            EmojiStringProperties.IsEmojiZwjSequence(value));
        _ = name;
    }
}
