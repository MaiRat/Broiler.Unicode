namespace UnicodeEmoji.StringProperties.Generator;

/// <summary>
/// Resolves well-known repository paths relative to a located solution root, so the CLI tools can be
/// run from anywhere (for example via <c>dotnet run</c>) without hard-coded absolute paths.
/// </summary>
public static class RepoLayout
{
    private static readonly string[] SolutionFileNames =
    {
        "UnicodeEmoji.StringProperties.slnx",
        "UnicodeEmoji.StringProperties.sln",
    };

    /// <summary>Walks up from <paramref name="start"/> until the solution file is found.</summary>
    /// <param name="start">The directory to start searching from. Defaults to the current directory.</param>
    /// <returns>The repository root directory.</returns>
    /// <exception cref="DirectoryNotFoundException">The solution root could not be located.</exception>
    public static string FindRepoRoot(string? start = null)
    {
        var dir = new DirectoryInfo(start ?? Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            foreach (string name in SolutionFileNames)
            {
                if (File.Exists(Path.Combine(dir.FullName, name)))
                {
                    return dir.FullName;
                }
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the solution file walking up from '{start ?? Directory.GetCurrentDirectory()}'.");
    }

    /// <summary>The directory holding downloaded Unicode data for a version: <c>data/unicode/&lt;version&gt;</c>.</summary>
    public static string DataDirectory(string repoRoot, string version) =>
        Path.Combine(repoRoot, "data", "unicode", version);

    /// <summary>The path of the committed generated trie source file.</summary>
    public static string GeneratedSourcePath(string repoRoot) =>
        Path.Combine(repoRoot, "src", "UnicodeEmoji.StringProperties", "Generated", "EmojiTrieData.g.cs");
}
