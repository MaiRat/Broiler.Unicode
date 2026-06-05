using System.Text;

namespace UnicodeEmoji.StringProperties.Tests;

/// <summary>Helpers for building emoji strings from scalar values without relying on source encoding.</summary>
internal static class TestText
{
    /// <summary>Builds a string from Unicode scalar values (code points).</summary>
    public static string FromScalars(params int[] scalars)
    {
        var sb = new StringBuilder(scalars.Length * 2);
        foreach (int scalar in scalars)
        {
            sb.Append(char.ConvertFromUtf32(scalar));
        }

        return sb.ToString();
    }
}
