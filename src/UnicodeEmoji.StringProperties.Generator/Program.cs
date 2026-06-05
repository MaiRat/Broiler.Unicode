using UnicodeEmoji.StringProperties.Generator;

// Generates the EmojiTrieData source file from already-downloaded Unicode data.
//
// Usage:
//   dotnet run --project src/UnicodeEmoji.StringProperties.Generator -- [<version>] [<dataDir>] [<outputPath>]
//
// With no arguments it generates for the default version using the conventional repository paths.

string version = args.Length > 0 ? args[0] : "16.0";

string repoRoot;
try
{
    repoRoot = RepoLayout.FindRepoRoot();
}
catch (DirectoryNotFoundException)
{
    repoRoot = Directory.GetCurrentDirectory();
}

string dataDir = args.Length > 1 ? args[1] : RepoLayout.DataDirectory(repoRoot, version);
string outputPath = args.Length > 2 ? args[2] : RepoLayout.GeneratedSourcePath(repoRoot);

if (!Directory.Exists(dataDir))
{
    Console.Error.WriteLine($"Data directory not found: {dataDir}");
    Console.Error.WriteLine("Download it first with the DataTool, e.g.:");
    Console.Error.WriteLine($"  dotnet run --project src/UnicodeEmoji.StringProperties.DataTool -- download {version}");
    return 1;
}

GenerationResult result = EmojiCodeGenerator.Generate(dataDir, version, outputPath);

Console.WriteLine($"Generated {result.OutputPath}");
Console.WriteLine($"  Unicode emoji version : {version}");
Console.WriteLine($"  Sequences             : {result.SequenceCount}");
Console.WriteLine($"  Trie nodes            : {result.NodeCount}");
Console.WriteLine($"  Trie edges            : {result.EdgeCount}");
Console.WriteLine($"  Max sequence length   : {result.MaxSequenceLength}");
return 0;
