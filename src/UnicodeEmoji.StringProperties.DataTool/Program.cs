using UnicodeEmoji.StringProperties.Generator;

// DataTool: downloads official Unicode emoji data files and (re)generates the lookup tables.
//
// Usage:
//   dotnet run --project src/UnicodeEmoji.StringProperties.DataTool -- download <version>
//   dotnet run --project src/UnicodeEmoji.StringProperties.DataTool -- generate <version>
//   dotnet run --project src/UnicodeEmoji.StringProperties.DataTool -- update   <version>   (download + generate)
//
// <version> is a Unicode emoji version such as "16.0". Files are stored under data/unicode/<version>/.

string[] DataFiles = ["emoji-test.txt", "emoji-sequences.txt", "emoji-zwj-sequences.txt"];

if (args.Length < 1)
{
    PrintUsage();
    return 1;
}

string command = args[0].ToLowerInvariant();
string version = args.Length > 1 ? args[1] : "16.0";

string repoRoot;
try
{
    repoRoot = RepoLayout.FindRepoRoot();
}
catch (DirectoryNotFoundException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

string dataDir = RepoLayout.DataDirectory(repoRoot, version);

switch (command)
{
    case "download":
        await DownloadAsync(version, dataDir);
        return 0;

    case "generate":
        Generate(version, dataDir, repoRoot);
        return 0;

    case "update":
        await DownloadAsync(version, dataDir);
        Generate(version, dataDir, repoRoot);
        return 0;

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintUsage();
        return 1;
}

async Task DownloadAsync(string version, string dataDir)
{
    Directory.CreateDirectory(dataDir);
    using var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.ParseAdd("UnicodeEmoji.StringProperties.DataTool/1.0");

    foreach (string file in DataFiles)
    {
        string url = $"https://www.unicode.org/Public/emoji/{version}/{file}";
        Console.WriteLine($"Downloading {url}");
        string content = await http.GetStringAsync(url);

        if (content.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("404 Not Found", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unexpected (HTML) response for {url}; is version '{version}' valid?");
        }

        // Normalize to LF for stable, diff-friendly committed data files.
        string destination = Path.Combine(dataDir, file);
        await File.WriteAllTextAsync(destination, content.Replace("\r\n", "\n"));
        Console.WriteLine($"  -> {destination}");
    }
}

void Generate(string version, string dataDir, string repoRoot)
{
    if (!Directory.Exists(dataDir))
    {
        throw new DirectoryNotFoundException($"Data directory not found: {dataDir}. Run 'download {version}' first.");
    }

    string outputPath = RepoLayout.GeneratedSourcePath(repoRoot);
    GenerationResult result = EmojiCodeGenerator.Generate(dataDir, version, outputPath);
    Console.WriteLine($"Generated {result.OutputPath}");
    Console.WriteLine($"  sequences={result.SequenceCount} nodes={result.NodeCount} edges={result.EdgeCount} maxLen={result.MaxSequenceLength}");
}

void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  download <version>   Download Unicode emoji data files into data/unicode/<version>/");
    Console.Error.WriteLine("  generate <version>   Generate the lookup tables from downloaded data");
    Console.Error.WriteLine("  update   <version>   Download then generate");
}
