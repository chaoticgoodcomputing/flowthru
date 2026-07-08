// Deployment-story spike (issue #140): the smallest real consumer of
// Flowthru.Extensions.DuckDB — seed a Parquet file, run a composite-key
// sort through the extension's engine (Parquet → sort → Parquet, no rows
// in the CLR), and verify the output. Exercises the DuckDB.NET P/Invoke
// path plus the extension's DI wiring, options, and schema verifier.
//
// Exit code 0 = transform ran and output verified sorted.

using DuckDB.NET.Data;
using Flowthru.Data.Storage;
using Flowthru.Hosting;
using Flowthru.Step.DuckDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

const int RowCount = 100_000;

Console.WriteLine($"runtime: {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}, " +
  $"dynamic-code={System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported} " +
  $"(false ⇒ NativeAOT)");

var root = Path.Combine(Path.GetTempPath(), $"duckdb-aot-spike-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
var inputPath = Path.Combine(root, "events.parquet");
var outputPath = Path.Combine(root, "sorted.parquet");

try
{
  // ── 1. Seed unsorted input Parquet with the raw provider ────────────────
  using (var seed = new DuckDBConnection("DataSource=:memory:"))
  {
    seed.Open();
    using var cmd = seed.CreateCommand();
    cmd.CommandText = $"""
      COPY (
        SELECT (hash(i) % 1000000)::BIGINT AS Id,
               'user_' || (i % 97)::VARCHAR AS Name
        FROM range({RowCount}) t(i)
      ) TO '{inputPath}' (FORMAT PARQUET)
      """;
    cmd.ExecuteNonQuery();

    // What DuckDB picks when no memory_limit is set (claimed: 80% of RAM).
    // Run the binary under a container memory cap to see whether the
    // default respects cgroup limits — load-bearing for sizing guidance
    // on memory-constrained hosts (Fargate, Lambda).
    cmd.CommandText = "SELECT current_setting('memory_limit')";
    Console.WriteLine($"engine default memory_limit (no explicit setting): {cmd.ExecuteScalar()}");
  }
  Console.WriteLine($"seeded {RowCount:N0} rows -> {inputPath}");

  // ── 2. The extension's engine, resolved the way a consumer gets it ──────
  // The Flowthru:DuckDb section is populated deliberately: UseDuckDb()'s
  // ConfigurationBinder.Bind is the extension's only AOT-flagged call site
  // (IL2026/IL3050 — reflection-based binder), so the spike must prove the
  // section actually binds under NativeAOT, not just no-op on empty config.
  var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["Flowthru:DuckDb:Threads"] = "3",
      ["Flowthru:DuckDb:MaxConcurrentTransforms"] = "2",
    })
    .Build();
  var services = new ServiceCollection();
  services.AddSingleton<IConfiguration>(configuration);
  new FlowthruServiceBuilder(services).UseDuckDb(opts =>
  {
    opts.MemoryLimit = "256MB";
    opts.TempDirectory = root;
  });
  await using var provider = services.BuildServiceProvider();

  var bound = provider
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<DuckDbEngineOptions>>()
    .Value;
  var sectionBound = bound.Threads == 3 && bound.MaxConcurrentTransforms == 2;
  Console.WriteLine($"options: Threads={bound.Threads?.ToString() ?? "null"} " +
    $"(expect 3, from config section), MaxConcurrentTransforms={bound.MaxConcurrentTransforms} " +
    $"(expect 2, from config section), MemoryLimit={bound.MemoryLimit} (expect 256MB, code-first)");
  Console.WriteLine(sectionBound
    ? "config-binding: OK — Flowthru:DuckDb section bound"
    : "config-binding: SILENT NO-OP — Flowthru:DuckDb section ignored " +
      "(known NativeAOT limitation: reflection-based ConfigurationBinder.Bind; " +
      "code-first UseDuckDb(opts => ...) values still apply)");

  var engine = provider.GetRequiredService<IDuckDbEngine>();

  // ── 3. Parquet → sort → Parquet, entirely engine-side ───────────────────
  var request = new DuckDbTransformRequest(
    StepLabel: "aot_sort",
    Relations: [new DuckDbBoundRelation("events", new ByteLocation.LocalFile(inputPath))],
    Sql: "SELECT Id, Name FROM events ORDER BY Id, Name",
    OutputLocation: new ByteLocation.LocalFile(outputPath),
    ExpectedColumns:
    [
      new DuckDbExpectedColumn("Id", typeof(long), IsNullable: false),
      new DuckDbExpectedColumn("Name", typeof(string), IsNullable: false),
    ],
    Options: DuckDbTransformOptions.Default
  );

  var result = await engine.ExecuteTransform(request).Run();

  return result.Match(
    onSuccess: ok =>
    {
      Console.WriteLine($"transform ok: rows={ok.RowsCopied:N0}, " +
        $"columns=[{string.Join(", ", ok.ResultColumns.Select(c => $"{c.Name}:{c.DuckDbType}"))}]");

      // ── 4. Verify the output really is sorted ──────────────────────────
      using var verify = new DuckDBConnection("DataSource=:memory:");
      verify.Open();
      using var cmd = verify.CreateCommand();
      cmd.CommandText = $"SELECT Id FROM read_parquet('{outputPath}')";
      using var reader = cmd.ExecuteReader();
      long prev = long.MinValue, rows = 0;
      while (reader.Read())
      {
        var id = reader.GetInt64(0);
        if (id < prev)
        {
          Console.Error.WriteLine($"FAIL: output not sorted at row {rows} ({id} < {prev})");
          return 2;
        }
        prev = id;
        rows++;
      }

      Console.WriteLine($"verified: {rows:N0} rows, Id non-decreasing. PASS");
      return 0;
    },
    onFailure: err =>
    {
      Console.Error.WriteLine($"FAIL: {err}");
      return 1;
    });
}
finally
{
  try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
}
