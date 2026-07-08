using System.Globalization;
using DuckDB.NET.Data;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.DuckDb;

namespace Flowthru.Step.DuckDb.Internal;

/// <summary>
/// The shipped <see cref="IDuckDbEngine"/>: an embedded, in-process
/// DuckDB. Each transform opens a fresh in-memory database, binds every
/// input relation as a view over <c>read_parquet(...)</c>, verifies the
/// SQL's described result schema against the declared output schema,
/// and writes the result with <c>COPY (...) TO ... (FORMAT PARQUET)</c>.
/// Rows never enter the .NET runtime — the only reader loop below walks
/// <c>DESCRIBE</c> output, which is one metadata row per <em>column</em>,
/// never per data row.
/// </summary>
/// <remarks>
/// <para>
/// A fresh database per transform keeps transforms hermetic (no view
/// name collisions, no state bleed between steps) at negligible cost —
/// DuckDB's in-memory startup is microseconds against transform bodies
/// that process millions of rows.
/// </para>
/// <para>
/// Schema verification runs <em>before</em> the <c>COPY</c>, so a
/// mismatched transform never writes a partial or mis-shaped output
/// file. <c>DESCRIBE</c> only binds the query — no input data is read
/// to answer it.
/// </para>
/// </remarks>
public sealed class InProcessDuckDbEngine : IDuckDbEngine
{
  private readonly DuckDbEngineOptions _options;

  public InProcessDuckDbEngine(DuckDbEngineOptions? options = null)
  {
    _options = options ?? new DuckDbEngineOptions();
    if (_options.MaxConcurrentTransforms < 1)
    {
      throw new ArgumentOutOfRangeException(
        nameof(options),
        _options.MaxConcurrentTransforms,
        "DuckDbEngineOptions.MaxConcurrentTransforms must be at least 1."
      );
    }
  }

  /// <inheritdoc/>
  public int MaxConcurrency => _options.MaxConcurrentTransforms;

  /// <inheritdoc/>
  public FlowIO<DuckDbTransformResult> ExecuteTransform(DuckDbTransformRequest request)
  {
    if (request is null) throw new ArgumentNullException(nameof(request));

    return FlowIO.LiftAsync(
        ct => RunTransformAsync(request, ct),
        source: $"DuckDbEngine.ExecuteTransform[{request.StepLabel}]"
      )
      .MapError(error => Translate(error, request.StepLabel));
  }

  // ── Transform body ──────────────────────────────────────────────────────

  private async Task<DuckDbTransformResult> RunTransformAsync(
    DuckDbTransformRequest request,
    CancellationToken ct
  )
  {
    var sql = TrimTerminator(request.Sql);

    using var connection = new DuckDBConnection("Data Source=:memory:");
    await connection.OpenAsync(ct).ConfigureAwait(false);

    await ApplyEngineSettingsAsync(connection, ct).ConfigureAwait(false);

    // Bind each input as a view over its Parquet file. The view is a
    // pure query definition — nothing is read until the transform runs.
    foreach (var relation in request.Relations)
    {
      await ExecuteAsync(
        connection,
        $"CREATE VIEW {QuoteIdentifier(relation.Name)} AS "
        + $"SELECT * FROM read_parquet({QuoteLiteral(relation.LocalPath)})",
        ct
      ).ConfigureAwait(false);
    }

    // Describe the result schema (binds the query without executing it)
    // and verify it against the declared output schema before any write.
    var resultColumns = await DescribeAsync(connection, sql, ct).ConfigureAwait(false);
    var mismatch = DuckDbSchemaVerifier.Verify(request.ExpectedColumns, resultColumns);
    if (mismatch is not null)
    {
      // Thrown here, translated to the typed RuntimeError.SchemaMismatch
      // value at the FlowIO boundary below — the same path a format
      // adapter's schema failure takes.
      throw new SchemaMismatchException(
        $"Transform SQL for step '{request.StepLabel}' produces a result schema that "
        + $"doesn't match the output item's declared schema: {mismatch}."
      );
    }

    // Execute the transform and write the result file inside the engine.
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!);
    var rowsCopied = await CopyToParquetAsync(
      connection, sql, request.OutputPath, request.Options, ct
    ).ConfigureAwait(false);

    return new DuckDbTransformResult(rowsCopied, resultColumns);
  }

  private async Task ApplyEngineSettingsAsync(DuckDBConnection connection, CancellationToken ct)
  {
    if (_options.MemoryLimit is { } memoryLimit)
    {
      await ExecuteAsync(connection, $"SET memory_limit={QuoteLiteral(memoryLimit)}", ct)
        .ConfigureAwait(false);
    }
    if (_options.Threads is { } threads)
    {
      await ExecuteAsync(
        connection, $"SET threads={threads.ToString(CultureInfo.InvariantCulture)}", ct
      ).ConfigureAwait(false);
    }
    if (_options.TempDirectory is { } tempDirectory)
    {
      await ExecuteAsync(connection, $"SET temp_directory={QuoteLiteral(tempDirectory)}", ct)
        .ConfigureAwait(false);
    }
  }

  private static async Task<IReadOnlyList<(string Name, string DuckDbType)>> DescribeAsync(
    DuckDBConnection connection,
    string sql,
    CancellationToken ct
  )
  {
    using var command = connection.CreateCommand();
    command.CommandText = $"DESCRIBE {sql}";

    var columns = new List<(string, string)>();
    using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
    // One row per RESULT COLUMN (schema metadata), not per data row —
    // DESCRIBE binds the query without reading input data.
    while (await reader.ReadAsync(ct).ConfigureAwait(false))
    {
      columns.Add((reader.GetString(0), reader.GetString(1)));
    }
    return columns;
  }

  private static async Task<long> CopyToParquetAsync(
    DuckDBConnection connection,
    string sql,
    string outputPath,
    DuckDbTransformOptions options,
    CancellationToken ct
  )
  {
    var copyOptions = $"FORMAT PARQUET, COMPRESSION {CompressionName(options.Compression)}";
    if (options.RowGroupSize is { } rowGroupSize)
    {
      copyOptions += $", ROW_GROUP_SIZE {rowGroupSize.ToString(CultureInfo.InvariantCulture)}";
    }

    using var command = connection.CreateCommand();
    command.CommandText =
      $"COPY ({sql}) TO {QuoteLiteral(Path.GetFullPath(outputPath))} ({copyOptions})";

    // DuckDB reports the copied-row count as COPY's scalar result; fall
    // back to 0 (informational only) if a future engine stops doing so.
    var scalar = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
    return scalar is null or DBNull ? 0L : Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
  }

  private static async Task ExecuteAsync(
    DuckDBConnection connection,
    string sql,
    CancellationToken ct
  )
  {
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
  }

  // ── Error translation ───────────────────────────────────────────────────

  /// <summary>
  /// Lift engine failures out of the generic
  /// <see cref="RuntimeError.External"/> envelope into their typed
  /// forms: a <see cref="SchemaMismatchException"/> becomes
  /// <see cref="RuntimeError.SchemaMismatch"/> (the same variant format
  /// adapters use) and a <see cref="DuckDBException"/> becomes the
  /// extension's <see cref="DuckDbRuntimeError.EngineFailed"/>. Other
  /// errors pass through unchanged.
  /// </summary>
  private static RuntimeError Translate(RuntimeError error, string stepLabel) =>
    error switch
    {
      RuntimeError.External { Cause: SchemaMismatchException smx } external =>
        new RuntimeError.SchemaMismatch(
          Source: external.Source,
          Detail: smx.Message,
          InnerExceptionInfo: smx.InnerException?.ToString()
        ),
      RuntimeError.External { Cause: DuckDBException dde } =>
        new RuntimeError.ExtensionError(
          new DuckDbRuntimeError.EngineFailed(stepLabel, dde.Message)
        ),
      _ => error,
    };

  // ── SQL text assembly ───────────────────────────────────────────────────

  /// <summary>Strip trailing whitespace and statement terminators so the
  /// query embeds cleanly in <c>DESCRIBE ...</c> and <c>COPY (...)</c>.</summary>
  private static string TrimTerminator(string sql) => sql.TrimEnd().TrimEnd(';').TrimEnd();

  /// <summary>Quote an identifier (view name), doubling embedded quotes.</summary>
  private static string QuoteIdentifier(string identifier) =>
    $"\"{identifier.Replace("\"", "\"\"")}\"";

  /// <summary>Quote a string literal (file path, setting), doubling embedded quotes.</summary>
  private static string QuoteLiteral(string value) =>
    $"'{value.Replace("'", "''")}'";

  private static string CompressionName(DuckDbParquetCompression compression) =>
    compression switch
    {
      DuckDbParquetCompression.Snappy => "SNAPPY",
      DuckDbParquetCompression.Zstd => "ZSTD",
      DuckDbParquetCompression.Gzip => "GZIP",
      DuckDbParquetCompression.Uncompressed => "UNCOMPRESSED",
      _ => throw new ArgumentOutOfRangeException(nameof(compression), compression, null),
    };
}
