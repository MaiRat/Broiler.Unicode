using UnicodeCldr.LocaleData.Generator;

// DataTool: downloads official CLDR JSON data slices and (re)generates the lookup tables.
//
// Usage:
//   dotnet run --project src/UnicodeCldr.LocaleData.DataTool -- download <version>
//   dotnet run --project src/UnicodeCldr.LocaleData.DataTool -- generate <version>
//   dotnet run --project src/UnicodeCldr.LocaleData.DataTool -- update   <version>   (download + generate)
//
// <version> is a cldr-json release tag such as "48.2.0". Files are stored under
// data/cldr/<version>/<locale>/. Only a curated set of locales is fetched to keep
// the committed data small; extend Locales below to broaden coverage.

string[] Locales =
{
    "ar", "de", "en", "es", "fr", "it", "ja", "ko",
    "nl", "pl", "pt", "ru", "sv", "tr", "zh",
};

if (args.Length < 1)
{
    PrintUsage();
    return 1;
}

string command = args[0].ToLowerInvariant();
string version = args.Length > 1 ? args[1] : "48.2.0";

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
    using var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.ParseAdd("UnicodeCldr.LocaleData.DataTool/1.0");

    foreach (string locale in Locales)
    {
        string url = $"https://raw.githubusercontent.com/unicode-org/cldr-json/{version}/cldr-json/cldr-misc-full/main/{locale}/listPatterns.json";
        Console.WriteLine($"Downloading {url}");
        string content = await http.GetStringAsync(url);

        if (content.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("404: Not Found", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unexpected response for {url}; are version '{version}' and locale '{locale}' valid?");
        }

        string localeDir = Path.Combine(dataDir, locale);
        Directory.CreateDirectory(localeDir);
        string destination = Path.Combine(localeDir, "listPatterns.json");
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

    string outputPath = RepoLayout.GeneratedListSourcePath(repoRoot);
    ListGenerationResult result = CldrListCodeGenerator.Generate(dataDir, version, outputPath);
    Console.WriteLine($"Generated {result.OutputPath}");
    Console.WriteLine($"  locales={result.LocaleCount} entries={result.EntryCount}");
}

void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  download <version>   Download CLDR list-pattern data into data/cldr/<version>/");
    Console.Error.WriteLine("  generate <version>   Generate the lookup tables from downloaded data");
    Console.Error.WriteLine("  update   <version>   Download then generate");
}
