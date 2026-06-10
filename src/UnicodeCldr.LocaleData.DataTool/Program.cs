using System.Text.Json;
using System.Text.Json.Nodes;
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
        string localeDir = Path.Combine(dataDir, locale);

        // Per-locale list patterns (cldr-misc) and number formats (cldr-numbers).
        await DownloadFileAsync(http,
            $"https://raw.githubusercontent.com/unicode-org/cldr-json/{version}/cldr-json/cldr-misc-full/main/{locale}/listPatterns.json",
            Path.Combine(localeDir, "listPatterns.json"));
        await DownloadFileAsync(http,
            $"https://raw.githubusercontent.com/unicode-org/cldr-json/{version}/cldr-json/cldr-numbers-full/main/{locale}/numbers.json",
            Path.Combine(localeDir, "numbers.json"));

        // Currency symbols are trimmed to the curated set so the committed slice
        // stays small (the full file lists ~300 currencies).
        await DownloadTrimmedCurrenciesAsync(http,
            $"https://raw.githubusercontent.com/unicode-org/cldr-json/{version}/cldr-json/cldr-numbers-full/main/{locale}/currencies.json",
            Path.Combine(localeDir, "currencies.json"), locale);

        // Unit patterns are trimmed to the ECMA-402 sanctioned simple units.
        await DownloadTrimmedUnitsAsync(http,
            $"https://raw.githubusercontent.com/unicode-org/cldr-json/{version}/cldr-json/cldr-units-full/main/{locale}/units.json",
            Path.Combine(localeDir, "units.json"), locale);

        // Gregorian calendar data, trimmed to the day-period names.
        await DownloadTrimmedGregorianAsync(http,
            $"https://raw.githubusercontent.com/unicode-org/cldr-json/{version}/cldr-json/cldr-dates-full/main/{locale}/ca-gregorian.json",
            Path.Combine(localeDir, "ca-gregorian.json"), locale);

        // Relative-time fields, trimmed to the ECMA-402 sanctioned units.
        await DownloadTrimmedDateFieldsAsync(http,
            $"https://raw.githubusercontent.com/unicode-org/cldr-json/{version}/cldr-json/cldr-dates-full/main/{locale}/dateFields.json",
            Path.Combine(localeDir, "dateFields.json"), locale);
    }

    // Supplemental data (single files, all locales).
    string supplementalDir = Path.Combine(dataDir, "supplemental");
    foreach (string file in new[] { "plurals.json", "ordinals.json", "currencyData.json", "dayPeriods.json" })
    {
        string url = $"https://raw.githubusercontent.com/unicode-org/cldr-json/{version}/cldr-json/cldr-core/supplemental/{file}";
        await DownloadFileAsync(http, url, Path.Combine(supplementalDir, file));
    }
}

async Task DownloadTrimmedCurrenciesAsync(HttpClient http, string url, string destination, string locale)
{
    Console.WriteLine($"Downloading {url} (trimmed)");
    string content = await http.GetStringAsync(url);
    var root = JsonNode.Parse(content)!;
    var currencies = root["main"]?[locale]?["numbers"]?["currencies"]?.AsObject();

    var kept = new JsonObject();
    if (currencies is not null)
    {
        foreach (string code in CldrCurrencyParser.CuratedCurrencies)
        {
            if (currencies.TryGetPropertyValue(code, out var entry) && entry is not null)
            {
                kept[code] = entry.DeepClone();
            }
        }
    }

    var trimmed = new JsonObject
    {
        ["main"] = new JsonObject
        {
            [locale] = new JsonObject
            {
                ["numbers"] = new JsonObject { ["currencies"] = kept },
            },
        },
    };

    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    await File.WriteAllTextAsync(destination, trimmed.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"  -> {destination}");
}

async Task DownloadTrimmedUnitsAsync(HttpClient http, string url, string destination, string locale)
{
    Console.WriteLine($"Downloading {url} (trimmed)");
    string content = await http.GetStringAsync(url);
    var root = JsonNode.Parse(content)!;
    var units = root["main"]?[locale]?["units"]?.AsObject();

    var keptUnits = new JsonObject();
    if (units is not null)
    {
        foreach (string display in new[] { "long", "short", "narrow" })
        {
            if (units[display] is not JsonObject section)
            {
                continue;
            }

            var keptSection = new JsonObject();
            foreach (var entry in section)
            {
                int dash = entry.Key.IndexOf('-');
                if (dash >= 0 && CldrUnitsParser.CuratedUnits.Contains(entry.Key[(dash + 1)..]) && entry.Value is not null)
                {
                    keptSection[entry.Key] = entry.Value.DeepClone();
                }
            }

            keptUnits[display] = keptSection;
        }
    }

    var trimmed = new JsonObject
    {
        ["main"] = new JsonObject
        {
            [locale] = new JsonObject { ["units"] = keptUnits },
        },
    };

    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    await File.WriteAllTextAsync(destination, trimmed.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"  -> {destination}");
}

async Task DownloadTrimmedGregorianAsync(HttpClient http, string url, string destination, string locale)
{
    Console.WriteLine($"Downloading {url} (trimmed)");
    string content = await http.GetStringAsync(url);
    var root = JsonNode.Parse(content)!;
    var dayPeriods = root["main"]?[locale]?["dates"]?["calendars"]?["gregorian"]?["dayPeriods"];

    var trimmed = new JsonObject
    {
        ["main"] = new JsonObject
        {
            [locale] = new JsonObject
            {
                ["dates"] = new JsonObject
                {
                    ["calendars"] = new JsonObject
                    {
                        ["gregorian"] = new JsonObject
                        {
                            ["dayPeriods"] = dayPeriods?.DeepClone() ?? new JsonObject(),
                        },
                    },
                },
            },
        },
    };

    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    await File.WriteAllTextAsync(destination, trimmed.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"  -> {destination}");
}

async Task DownloadTrimmedDateFieldsAsync(HttpClient http, string url, string destination, string locale)
{
    Console.WriteLine($"Downloading {url} (trimmed)");
    string content = await http.GetStringAsync(url);
    var root = JsonNode.Parse(content)!;
    var fields = root["main"]?[locale]?["dates"]?["fields"]?.AsObject();

    // Keep only the ECMA-402 relative-time units, in their long/short/narrow forms.
    var keptFields = new JsonObject();
    if (fields is not null)
    {
        foreach (string unit in CldrRelativeTimeParser.Units)
        {
            foreach (string suffix in new[] { "", "-short", "-narrow" })
            {
                string key = unit + suffix;
                if (fields.TryGetPropertyValue(key, out var entry) && entry is not null)
                {
                    keptFields[key] = entry.DeepClone();
                }
            }
        }
    }

    var trimmed = new JsonObject
    {
        ["main"] = new JsonObject
        {
            [locale] = new JsonObject
            {
                ["dates"] = new JsonObject { ["fields"] = keptFields },
            },
        },
    };

    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    await File.WriteAllTextAsync(destination, trimmed.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"  -> {destination}");
}

async Task DownloadFileAsync(HttpClient http, string url, string destination)
{
    Console.WriteLine($"Downloading {url}");
    string content = await http.GetStringAsync(url);

    if (content.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("404: Not Found", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Unexpected response for {url}; is version valid?");
    }

    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    await File.WriteAllTextAsync(destination, content.Replace("\r\n", "\n"));
    Console.WriteLine($"  -> {destination}");
}

void Generate(string version, string dataDir, string repoRoot)
{
    if (!Directory.Exists(dataDir))
    {
        throw new DirectoryNotFoundException($"Data directory not found: {dataDir}. Run 'download {version}' first.");
    }

    ListGenerationResult list = CldrListCodeGenerator.Generate(dataDir, version, RepoLayout.GeneratedListSourcePath(repoRoot));
    Console.WriteLine($"Generated {list.OutputPath}");
    Console.WriteLine($"  locales={list.LocaleCount} entries={list.EntryCount}");

    PluralGenerationResult plural = CldrPluralCodeGenerator.Generate(
        RepoLayout.SupplementalDirectory(repoRoot, version), version, RepoLayout.GeneratedPluralSourcePath(repoRoot));
    Console.WriteLine($"Generated {plural.OutputPath}");
    Console.WriteLine($"  entries={plural.EntryCount}");

    CurrencyGenerationResult currency = CldrCurrencyCodeGenerator.Generate(
        dataDir, RepoLayout.SupplementalDirectory(repoRoot, version), version, RepoLayout.GeneratedCurrencySourcePath(repoRoot));
    Console.WriteLine($"Generated {currency.OutputPath}");
    Console.WriteLine($"  layouts={currency.LayoutCount} symbols={currency.SymbolCount} digits={currency.DigitsCount}");

    UnitsGenerationResult units = CldrUnitsCodeGenerator.Generate(dataDir, version, RepoLayout.GeneratedUnitSourcePath(repoRoot));
    Console.WriteLine($"Generated {units.OutputPath}");
    Console.WriteLine($"  entries={units.EntryCount}");

    DatesGenerationResult dates = CldrDatesCodeGenerator.Generate(
        dataDir, RepoLayout.SupplementalDirectory(repoRoot, version), version, RepoLayout.GeneratedDateSourcePath(repoRoot));
    Console.WriteLine($"Generated {dates.OutputPath}");
    Console.WriteLine($"  names={dates.NameCount} rules={dates.RuleCount}");

    RelativeTimeGenerationResult relativeTime = CldrRelativeTimeCodeGenerator.Generate(
        dataDir, version, RepoLayout.GeneratedRelativeTimeSourcePath(repoRoot));
    Console.WriteLine($"Generated {relativeTime.OutputPath}");
    Console.WriteLine($"  patterns={relativeTime.PatternCount} exact={relativeTime.ExactCount}");
}

void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  download <version>   Download CLDR list-pattern data into data/cldr/<version>/");
    Console.Error.WriteLine("  generate <version>   Generate the lookup tables from downloaded data");
    Console.Error.WriteLine("  update   <version>   Download then generate");
}
