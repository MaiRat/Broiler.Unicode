using UnicodeCldr.LocaleData.Generator;

// Generates the CldrListData source file from already-downloaded CLDR data.
//
// Usage:
//   dotnet run --project src/UnicodeCldr.LocaleData.Generator -- [<version>] [<dataDir>] [<outputPath>]

string version = args.Length > 0 ? args[0] : "48.2.0";

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
string outputPath = args.Length > 2 ? args[2] : RepoLayout.GeneratedListSourcePath(repoRoot);

if (!Directory.Exists(dataDir))
{
    Console.Error.WriteLine($"Data directory not found: {dataDir}");
    Console.Error.WriteLine("Download it first with the DataTool, e.g.:");
    Console.Error.WriteLine($"  dotnet run --project src/UnicodeCldr.LocaleData.DataTool -- download {version}");
    return 1;
}

ListGenerationResult result = CldrListCodeGenerator.Generate(dataDir, version, outputPath);

Console.WriteLine($"Generated {result.OutputPath}");
Console.WriteLine($"  CLDR version : {version}");
Console.WriteLine($"  Locales      : {result.LocaleCount}");
Console.WriteLine($"  Entries      : {result.EntryCount}");

string supplementalDir = RepoLayout.SupplementalDirectory(repoRoot, version);
if (Directory.Exists(supplementalDir))
{
    PluralGenerationResult plural = CldrPluralCodeGenerator.Generate(
        supplementalDir, version, RepoLayout.GeneratedPluralSourcePath(repoRoot));
    Console.WriteLine($"Generated {plural.OutputPath}");
    Console.WriteLine($"  Plural entries : {plural.EntryCount}");

    CurrencyGenerationResult currency = CldrCurrencyCodeGenerator.Generate(
        dataDir, supplementalDir, version, RepoLayout.GeneratedCurrencySourcePath(repoRoot));
    Console.WriteLine($"Generated {currency.OutputPath}");
    Console.WriteLine($"  Currency layouts : {currency.LayoutCount}, symbols : {currency.SymbolCount}");
}

return 0;
