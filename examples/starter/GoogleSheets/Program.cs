using Flowthru.Cli;
using Flowthru.Data.Storage.Sheets.Local;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using GoogleSheets.Data;
using GoogleSheets.Flows.Sales;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoogleSheets;

/// <summary>
/// Entry point for the Google Sheets starter. Reads a raw sales table from a
/// spreadsheet, totals it by day, and writes the result back to a second table —
/// all through the Sheets catalog, all offline.
/// </summary>
/// <remarks>
/// <para>
/// The whole example runs against a <see cref="JsonFileSheetsGateway"/>: no
/// Google account, no credentials, no network — just a local JSON file standing
/// in for the spreadsheet. The gateway is the swap point. In production you
/// replace the gateway-construction-and-seeding lines with a single
/// <c>builder.AddGoogleSheets(sheetsService)</c> over an authenticated
/// <c>SheetsService</c> (built from a service account, OAuth, or Application
/// Default Credentials) — the catalog, the flow, and the steps do not change.
/// </para>
/// <para>
/// Data lives under <c>Data/.local-sheets/</c>. <c>raw.json</c> is the
/// checked-in input — the spreadsheet's starting contents, which you can open
/// and edit. Each run copies it over <c>working.json</c> (gitignored) and points
/// the gateway there, so a run always starts from the pristine raw input. After
/// a run, open <c>working.json</c> to see the <c>RawSales</c> table plus the
/// <c>DailyTotals</c> table the flow created.
/// </para>
/// </remarks>
public class Program
{
  // The id of the spreadsheet the tables live in. Offline this is just a key in
  // the JSON store; against the live API it is the id from the sheet's URL.
  private const string SpreadsheetId = "example-spreadsheet";

  // The checked-in raw input sheet (the spreadsheet's starting contents) and the
  // derived working copy the gateway reads from and flushes to on each run.
  private static readonly string LocalSheetsDir =
    Path.Combine("Data", ".local-sheets");
  private const string RawFileName = "raw.json";
  private const string WorkingFileName = "working.json";

  public static Task<int> Main(string[] args)
  {
    var basePath = Directory.GetCurrentDirectory();
    var sheetsDir = Path.Combine(basePath, LocalSheetsDir);
    Directory.CreateDirectory(sheetsDir);

    // raw.json is the source of truth; working.json is the derived, mutated copy
    // you inspect. Start every interactive run from the pristine raw input.
    var rawPath = RequireRawPath(basePath);
    var workingPath = Path.Combine(sheetsDir, WorkingFileName);
    File.Copy(rawPath, workingPath, overwrite: true);

    return FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, basePath, workingPath)
    );
  }

  /// <summary>
  /// Build a configured service provider for tests / external hosts.
  /// </summary>
  /// <remarks>
  /// Each call copies the checked-in <c>raw.json</c> to a <strong>fresh temp
  /// working file</strong>, so the auto-discovered example test is deterministic,
  /// runs from the same pristine input the interactive path does, and never
  /// touches (or depends on) <c>Data/.local-sheets/working.json</c>.
  /// </remarks>
  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var resolvedBase = basePath ?? Directory.GetCurrentDirectory();
    // A unique temp working file per call keeps parallel test runs disjoint and
    // avoids polluting the project's inspectable working.json.
    var rawPath = RequireRawPath(resolvedBase);
    var workingPath = Path.Combine(
      Path.GetTempPath(), $"flowthru-sheets-{Guid.NewGuid():N}.json");
    File.Copy(rawPath, workingPath, overwrite: true);

    var services = new ServiceCollection();
    ConfigureServices(services, resolvedBase, workingPath);
    return services.BuildServiceProvider();
  }

  // Resolve the checked-in raw input sheet, failing clearly if it is missing.
  // It is committed, so a miss means the project layout is wrong, not a normal
  // state to recover from.
  private static string RequireRawPath(string basePath)
  {
    var rawPath = Path.Combine(basePath, LocalSheetsDir, RawFileName);
    if (!File.Exists(rawPath))
    {
      throw new FileNotFoundException(
        $"Raw input sheet not found at '{rawPath}'. It is checked in under "
        + $"{LocalSheetsDir}/{RawFileName}; the example cannot run without it.",
        rawPath);
    }

    return rawPath;
  }

  private static void ConfigureServices(
    IServiceCollection services, string basePath, string workingPath)
  {
    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    // ── The swap point ────────────────────────────────────────────────────
    // An offline, file-backed gateway over the working copy of the raw input
    // sheet (RawSales already lives in the JSON). Swap this whole block for
    // `builder.AddGoogleSheets(sheetsService)` to talk to a real sheet.
    var gateway = new JsonFileSheetsGateway(workingPath);

    services.AddFlowthru(flowthru =>
    {
      // Register the gateway (retry-on-429 wrapped by default), then hand the
      // same instance to the catalog so its Sheets items route through it. This
      // mirrors the EF Core example's injected-context pattern.
      flowthru.AddGoogleSheets(gateway);
      flowthru.RegisterCatalog(_ => new Catalog(gateway, SpreadsheetId));

      flowthru
        .RegisterFlow<Catalog>("Sales", SalesFlow.Create)
        .WithDescription("Totals raw sales by day and writes the result back to the spreadsheet");

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
