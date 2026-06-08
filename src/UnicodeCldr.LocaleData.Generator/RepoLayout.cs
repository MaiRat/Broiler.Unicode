namespace UnicodeCldr.LocaleData.Generator;

/// <summary>
/// Resolves well-known repository paths relative to a located solution root, so the
/// CLI tools can be run from anywhere (for example via <c>dotnet run</c>) without
/// hard-coded absolute paths.
/// </summary>
public static class RepoLayout
{
    private static readonly string[] SolutionFileNames =
    {
        "UnicodeEmoji.StringProperties.slnx",
        "UnicodeEmoji.StringProperties.sln",
    };

    /// <summary>Walks up from <paramref name="start"/> until the solution file is found.</summary>
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

    /// <summary>The directory holding downloaded CLDR data for a version: <c>data/cldr/&lt;version&gt;</c>.</summary>
    public static string DataDirectory(string repoRoot, string version) =>
        Path.Combine(repoRoot, "data", "cldr", version);

    /// <summary>The directory holding downloaded CLDR supplemental data: <c>data/cldr/&lt;version&gt;/supplemental</c>.</summary>
    public static string SupplementalDirectory(string repoRoot, string version) =>
        Path.Combine(DataDirectory(repoRoot, version), "supplemental");

    /// <summary>The path of the committed generated CLDR list-patterns source file.</summary>
    public static string GeneratedListSourcePath(string repoRoot) =>
        Path.Combine(repoRoot, "src", "UnicodeCldr.LocaleData", "Generated", "CldrListData.g.cs");

    /// <summary>The path of the committed generated CLDR plural-rules source file.</summary>
    public static string GeneratedPluralSourcePath(string repoRoot) =>
        Path.Combine(repoRoot, "src", "UnicodeCldr.LocaleData", "Generated", "CldrPluralData.g.cs");

    /// <summary>The path of the committed generated CLDR currency-data source file.</summary>
    public static string GeneratedCurrencySourcePath(string repoRoot) =>
        Path.Combine(repoRoot, "src", "UnicodeCldr.LocaleData", "Generated", "CldrCurrencyData.g.cs");

    /// <summary>The path of the committed generated CLDR unit-patterns source file.</summary>
    public static string GeneratedUnitSourcePath(string repoRoot) =>
        Path.Combine(repoRoot, "src", "UnicodeCldr.LocaleData", "Generated", "CldrUnitData.g.cs");
}
