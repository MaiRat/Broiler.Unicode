using System;
using System.Collections.Generic;

namespace Broiler.Unicode.Properties;

/// <summary>
/// Lookup of Unicode code-point sets for the binary properties supported by
/// ECMAScript regular-expression Unicode property escapes (<c>\p{…}</c>/<c>\P{…}</c>),
/// backed by compact range tables generated from the Unicode Character Database 17.0.0.
/// No data is read from disk and there is no native dependency.
/// </summary>
public static class UnicodeProperties
{
    private static readonly Dictionary<string, (int Lo, int Hi)[]> BinaryCache = new(StringComparer.Ordinal);
    private static readonly object Gate = new();

    /// <summary>
    /// Returns the sorted, non-overlapping code-point ranges for the named binary
    /// Unicode property, or <c>null</c> when the name is not a supported binary
    /// property. The name is matched loosely (case-insensitive, ignoring <c>_</c>,
    /// spaces and <c>-</c>), so both canonical names (e.g. <c>Alphabetic</c>) and the
    /// ECMAScript aliases (e.g. <c>Alpha</c>) resolve to the same set.
    /// </summary>
    public static (int Lo, int Hi)[]? GetBinaryProperty(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        var key = Normalize(name);
        lock (Gate)
        {
            if (BinaryCache.TryGetValue(key, out var cached))
                return cached;

            if (!BinaryPropertyData.Ranges.TryGetValue(key, out var encoded))
                return null;

            var parsed = Parse(encoded);
            BinaryCache[key] = parsed;
            return parsed;
        }
    }

    private static string Normalize(string s)
    {
        var buffer = new char[s.Length];
        var n = 0;
        foreach (var ch in s)
        {
            if (ch is '_' or ' ' or '-')
                continue;
            buffer[n++] = char.ToLowerInvariant(ch);
        }
        return new string(buffer, 0, n);
    }

    private static (int Lo, int Hi)[] Parse(string encoded)
    {
        if (encoded.Length == 0)
            return Array.Empty<(int, int)>();

        var count = 1;
        foreach (var ch in encoded)
        {
            if (ch == ',')
                count++;
        }

        var result = new (int Lo, int Hi)[count];
        var index = 0;
        var start = 0;
        for (var i = 0; i <= encoded.Length; i++)
        {
            if (i != encoded.Length && encoded[i] != ',')
                continue;

            var span = encoded.AsSpan(start, i - start);
            var dash = span.IndexOf('-');
            if (dash < 0)
            {
                var cp = ParseHex(span);
                result[index++] = (cp, cp);
            }
            else
            {
                var lo = ParseHex(span[..dash]);
                var hi = ParseHex(span[(dash + 1)..]);
                result[index++] = (lo, hi);
            }

            start = i + 1;
        }

        return result;
    }

    private static int ParseHex(ReadOnlySpan<char> span)
    {
        var value = 0;
        foreach (var ch in span)
        {
            value = (value << 4) + (ch <= '9' ? ch - '0' : (char.ToLowerInvariant(ch) - 'a') + 10);
        }
        return value;
    }
}
