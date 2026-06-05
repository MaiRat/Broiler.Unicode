using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnicodeEmoji.StringProperties.Generator;
using Xunit;

namespace UnicodeEmoji.StringProperties.Tests;

/// <summary>
/// Data-driven tests that exercise the generated trie against the very Unicode data files it was built
/// from. These guard against drift between the committed tables and the source data.
/// </summary>
public class UnicodeDataTests
{
    private static string DataDirectory()
    {
        string root = RepoLayout.FindRepoRoot(AppContext.BaseDirectory);
        return RepoLayout.DataDirectory(root, EmojiStringProperties.UnicodeEmojiVersion);
    }

    [Fact]
    public void EveryFullyQualifiedTestEntry_IsRgiEmoji()
    {
        string path = Path.Combine(DataDirectory(), "emoji-test.txt");
        var failures = new List<string>();
        int count = 0;

        foreach (EmojiTestEntry entry in UnicodeEmojiDataParser.ParseEmojiTest(path))
        {
            if (entry.Status != EmojiTestStatus.FullyQualified)
            {
                continue;
            }

            count++;
            string value = TestText.FromScalars(entry.Scalars);
            if (!EmojiStringProperties.IsRgiEmoji(value))
            {
                failures.Add($"{entry.Name} [{Hex(entry.Scalars)}]");
            }
        }

        Assert.True(count > 1000, $"Expected many fully-qualified entries, saw {count}.");
        Assert.True(failures.Count == 0, $"{failures.Count} fully-qualified entries were not RGI:\n" + string.Join('\n', failures.Take(20)));
    }

    [Fact]
    public void EveryParsedSequence_RoundTripsWithItsProperty()
    {
        IReadOnlyList<EmojiSequence> sequences = UnicodeEmojiDataParser.ParseSequences(DataDirectory());
        Assert.NotEmpty(sequences);

        foreach (EmojiSequence sequence in sequences)
        {
            string value = TestText.FromScalars(sequence.Scalars);
            EmojiSequenceProperties props = EmojiStringProperties.GetProperties(value);

            // The exact property bit recorded in the data must be present at runtime.
            var expectedBit = (EmojiSequenceProperties)(byte)sequence.Property;
            Assert.True(
                (props & expectedBit) == expectedBit,
                $"{sequence.Name} [{Hex(sequence.Scalars)}] expected {expectedBit} but got {props}");
        }
    }

    [Fact]
    public void MinimallyQualifiedEntries_AreNotClaimedAsRgi()
    {
        // Minimally-qualified / unqualified forms are missing presentation selectors and must NOT be
        // reported as RGI by the exact-match API (they are not in the RGI set).
        string path = Path.Combine(DataDirectory(), "emoji-test.txt");

        foreach (EmojiTestEntry entry in UnicodeEmojiDataParser.ParseEmojiTest(path))
        {
            if (entry.Status != EmojiTestStatus.MinimallyQualified && entry.Status != EmojiTestStatus.Unqualified)
            {
                continue;
            }

            string value = TestText.FromScalars(entry.Scalars);

            // If a minimally-qualified form happens to also be a fully-qualified RGI emoji elsewhere it
            // would be listed as fully-qualified, so these should not match exactly.
            Assert.False(
                EmojiStringProperties.IsRgiEmoji(value),
                $"Non-fully-qualified entry unexpectedly matched: {entry.Name} [{Hex(entry.Scalars)}]");
        }
    }

    private static string Hex(int[] scalars) => string.Join(' ', scalars.Select(c => c.ToString("X4")));
}
