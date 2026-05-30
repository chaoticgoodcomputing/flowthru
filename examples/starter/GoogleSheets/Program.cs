using Flowthru.Cli;
using Flowthru.Data.Storage.Sheets;
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
/// Interactively (<c>dotnet run</c>) the gateway writes to <c>./sheet.json</c> in
/// the project directory (gitignored). After a run, open it to see the seeded
/// <c>RawSales</c> table plus the <c>DailyTotals</c> table the flow created.
/// </para>
/// </remarks>
public class Program
{
  // The id of the spreadsheet the tables live in. Offline this is just a key in
  // the JSON store; against the live API it is the id from the sheet's URL.
  private const string SpreadsheetId = "example-spreadsheet";

  // The local file the offline gateway reads from and flushes to on each run.
  private const string SheetFileName = "sheet.json";

  public static Task<int> Main(string[] args)
  {
    var basePath = Directory.GetCurrentDirectory();
    var sheetPath = Path.Combine(basePath, SheetFileName);
    return FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, basePath, sheetPath)
    );
  }

  /// <summary>
  /// Build a configured service provider for tests / external hosts.
  /// </summary>
  /// <remarks>
  /// Each call points the gateway at a <strong>fresh temp file</strong>, seeded
  /// from scratch, so the auto-discovered example test is deterministic and never
  /// touches (or depends on) the interactive <c>./sheet.json</c>.
  /// </remarks>
  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var resolvedBase = basePath ?? Directory.GetCurrentDirectory();
    // A unique temp file per call keeps parallel test runs disjoint and avoids
    // polluting the project's inspectable ./sheet.json.
    var sheetPath = Path.Combine(
      Path.GetTempPath(), $"flowthru-sheets-{Guid.NewGuid():N}.json");

    var services = new ServiceCollection();
    ConfigureServices(services, resolvedBase, sheetPath);
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(
    IServiceCollection services, string basePath, string sheetPath)
  {
    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    // ── The swap point ────────────────────────────────────────────────────
    // An offline, file-backed gateway, seeded with the input the flow reads.
    // Swap this whole block for `builder.AddGoogleSheets(sheetsService)` to talk
    // to a real sheet.
    var gateway = new JsonFileSheetsGateway(sheetPath);
    SeedFixture(gateway);

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

  /// <summary>
  /// Stand in for the spreadsheet's existing contents. Offline, the input table
  /// has to exist before the flow reads it — Flowthru creates tables, not
  /// spreadsheets, so the spreadsheet must be registered first. The output table
  /// is left absent on purpose: the flow's write creates it from the schema,
  /// demonstrating the create-if-absent "Raw Data" pattern.
  /// </summary>
  /// <remarks>
  /// Idempotent: if the gateway's file already holds <c>RawSales</c> (a prior
  /// interactive run), seeding is skipped so a re-run reads the existing input
  /// rather than throwing on the duplicate table.
  /// </remarks>
  private static void SeedFixture(JsonFileSheetsGateway gateway)
  {
    // Reachable spreadsheet — the offline analogue of a sheet existing in Drive.
    gateway.RegisterSpreadsheet(SpreadsheetId);

    // Already seeded (e.g. a prior run flushed it to disk)? Leave it alone.
    if (gateway.ResolveTable(SpreadsheetId, "RawSales", default).GetAwaiter().GetResult() is not null)
    {
      return;
    }

    // The input table's column names match RawSaleSchema's serialized labels;
    // the date column is seeded as a natural Temporal field. The gateway
    // normalizes Date/DateTime/Time columns to the serial Number the live API
    // returns on read, so the schema-driven decoder coerces it back to a
    // DateOnly either way.
    var schema = new TableSchema(new[]
    {
      new TableColumn("Product", ColumnType.Text),
      new TableColumn("SoldOn", ColumnType.Date),
      new TableColumn("Amount", ColumnType.Number),
    });

    var rows = new[]
    {
      Row("Widget", new DateOnly(2026, 5, 1), 10.00),
      Row("Gadget", new DateOnly(2026, 5, 1), 5.50),
      Row("Widget", new DateOnly(2026, 5, 2), 12.25),
    };

    gateway.Seed(SpreadsheetId, "RawSales", schema, rows);
  }

  private static IReadOnlyList<FieldValue> Row(string product, DateOnly soldOn, double amount) =>
    new[]
    {
      FieldValue.Text(product),
      FieldValue.Temporal(soldOn.ToDateTime(TimeOnly.MinValue), TemporalKind.Date),
      FieldValue.Number(amount),
    };
}
