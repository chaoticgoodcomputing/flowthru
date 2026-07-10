using System.Globalization;
using Flowthru.Cli;
using Microsoft.Extensions.Configuration;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Flowthru.Step.DuckDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WideTransformBenchmark.Benchmark;
using WideTransformBenchmark.Data;
using WideTransformBenchmark.Data._04_Reporting.Schemas;
using WideTransformBenchmark.Flows.Analyze;
using WideTransformBenchmark.Flows.EagerOptimize;
using WideTransformBenchmark.Flows.EngineOptimize;

namespace WideTransformBenchmark;

/// <summary>
/// Entry point with two personalities:
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><c>dotnet run</c> (no arguments) — the bespoke benchmark harness:
///         fabricate the datasets if missing, re-measure the optimize pass
///         through both transform paths at every size (measurement is never
///         reused — see <see cref="BenchmarkRunner"/> for the cache-correctness
///         guarantees), stage the measurement rows as a Raw CSV, run the
///         Analyze Flow over them, and print the verdict.</item>
///   <item><c>dotnet run -- &lt;anything&gt;</c> — the standard Flowthru CLI
///         (<c>--dry-run</c>, <c>--help</c>, flow selection), over the same
///         registrations. <see cref="ConfigureServices(string?)"/> stages the
///         benchmark first if its outputs are missing, so pre-flight always
///         finds real inputs; it is also the seam the example integration
///         harness discovers and runs.</item>
/// </list>
/// </remarks>
public static class Program
{
  public static async Task<int> Main(string[] args)
  {
    var basePath = Directory.GetCurrentDirectory();

    if (args.Length > 0)
    {
      return await FlowthruCli.RunStandaloneAsync(
        args,
        services => ConfigureServices(services, basePath));
    }

    return await RunBenchmarkAsync(basePath);
  }

  /// <summary>
  /// Configures services for hosted runs. Used by the CLI verbs above and by
  /// the example integration test infrastructure (which passes the project
  /// directory as <paramref name="basePath"/>).
  /// </summary>
  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    var sizes = BenchmarkRunner.ResolveSizes();
    var dataPath = Path.Combine(basePath, "Data");

    // The staging step, FlowthruCoverage-style: before the hosted flows are
    // even registered, make sure their inputs exist. For this example the
    // staging work IS running the benchmark — fabricate any missing dataset
    // and, if the measurement CSV is absent (or measured for different
    // sizes), run the measured benchmark once to produce it. Sync-over-async
    // is confined to this configure-time seam; Task.Run keeps it safe under
    // test hosts that install a SynchronizationContext.
    Task.Run(() => BenchmarkRunner.EnsureStagedAsync(dataPath, sizes)).GetAwaiter().GetResult();

    // UseDuckDb binds its engine options from the Flowthru:DuckDb section, so
    // an IConfiguration must exist. No file is required — drop an optional
    // appsettings.json next to the project to tune MemoryLimit and friends.
    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(sp => new Catalog(dataPath));

      // Registers the embedded engine and the hermetic pre-flight check that
      // binds the optimize pass's SQL against the declared Schemas.
      flowthru.UseDuckDb();

      // Deliberately NO UseCacheStorage() anywhere in this project: with no
      // cache manifest there is no cache plan, so hosted runs execute every
      // step every time — the same guarantee the harness relies on for its
      // measured runs. Adding caching here would make a second hosted run
      // serve benchmark outputs without executing the transforms.

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        // One merged diagram (rather than per-flow) — the shared raw items
        // feeding both transform paths are the picture worth drawing here.
        meta.AddMermaidMetadata(opt => opt
          .WithOutputDirectory(metadataPath)
          .WithPerFlow(perFlow => perFlow.WithMode(PerFlowMode.Disabled)));
      });

      foreach (var size in sizes)
      {
        // One shard catalog per size, closure-captured by the per-size flow
        // factories (the RetailDataSplitFlow shard pattern).
        var sized = new SizedBenchmarkCatalog(dataPath, size);

        flowthru
          .RegisterFlow<Catalog>($"EagerOptimize_{size}", _ => EagerOptimizeFlow.Create(sized))
          .WithDescription($"Optimize pass over {size:N0} rows as an eager C# LINQ Step");

        flowthru
          .RegisterFlow<Catalog, IDuckDbEngine>(
            $"EngineOptimize_{size}",
            (_, engine) => EngineOptimizeFlow.Create(sized, engine))
          .WithDescription($"Optimize pass over {size:N0} rows as a DuckDB engine transform");
      }

      flowthru
        .RegisterFlow<Catalog>("Analyze", AnalyzeFlow.Create)
        .WithDescription(
          "Ingests the staged measurement rows and renders benchmark_summary.csv + benchmark_report.md");
    });
  }

  // ── The bespoke benchmark path (dotnet run, no args) ─────────────────────

  private static async Task<int> RunBenchmarkAsync(string basePath)
  {
    var sizes = BenchmarkRunner.ResolveSizes();
    var dataPath = Path.Combine(basePath, "Data");
    var catalog = new Catalog(dataPath);

    Console.WriteLine(
      $"Wide-transform benchmark: sizes [{string.Join(", ", sizes.Select(s => s.ToString("N0", CultureInfo.InvariantCulture)))}]"
      + $" (override via {BenchmarkRunner.SizesEnvVar})\n");

    // Measure fresh every run — a benchmark that reuses yesterday's numbers
    // isn't one. Fabrication is the only thing reused across runs (the
    // generator is seeded and deterministic, so the file is identical anyway).
    var measurements = await BenchmarkRunner.MeasureAsync(dataPath, sizes);
    await BenchmarkRunner.SaveMeasurementsAsync(catalog, measurements);

    // The dogfood: an ordinary Flowthru Flow ingests the measurement rows the
    // harness just staged and renders the deliverables.
    await BenchmarkRunner.RunFlowOrThrow(AnalyzeFlow.Create(catalog), "Analyze flow");

    await PrintSummaryAsync(catalog, dataPath);
    return 0;
  }

  private static async Task PrintSummaryAsync(Catalog catalog, string dataPath)
  {
    var outcome = await catalog.BenchmarkSummary.Load().Run();
    if (outcome is not Flowthru.Prelude.EffResult<IEnumerable<BenchmarkComparison>>.Success success)
    {
      throw new InvalidOperationException($"Loading benchmark summary failed: {outcome}");
    }

    var ci = CultureInfo.InvariantCulture;
    Console.WriteLine();
    Console.WriteLine("──────────────────────────────────────────────────────────────────────────");
    Console.WriteLine("  Input rows   Output rows   Eager ms   Engine ms   Speedup   Alloc ratio");
    foreach (var c in success.Value.OrderBy(c => c.InputRows))
    {
      Console.WriteLine(
        $"  {c.InputRows,10:N0}   {c.OutputRows,11:N0}   {c.EagerMs,8:N0}   {c.EngineMs,9:N0}"
        + $"   {c.SpeedupX.ToString("0.00", ci),6}x   {c.AllocationRatioX.ToString("0.0", ci),9}x");
    }
    Console.WriteLine("──────────────────────────────────────────────────────────────────────────");
    Console.WriteLine($"  Report:  {Path.Combine(dataPath, "_04_Reporting", "Datasets", "benchmark_report.md")}");
    Console.WriteLine($"  Summary: {Path.Combine(dataPath, "_04_Reporting", "Datasets", "benchmark_summary.csv")}");
    Console.WriteLine();
  }
}
