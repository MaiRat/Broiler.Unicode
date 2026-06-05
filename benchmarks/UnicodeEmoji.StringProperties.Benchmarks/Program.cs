using BenchmarkDotNet.Running;
using UnicodeEmoji.StringProperties.Benchmarks;

// Run all benchmarks, or a subset via the BenchmarkDotNet command-line filter, e.g.:
//   dotnet run -c Release --project benchmarks/UnicodeEmoji.StringProperties.Benchmarks -- --filter *ExactMatch*
BenchmarkSwitcher
    .FromTypes([typeof(ExactMatchBenchmarks), typeof(EnumerationBenchmarks)])
    .Run(args);
