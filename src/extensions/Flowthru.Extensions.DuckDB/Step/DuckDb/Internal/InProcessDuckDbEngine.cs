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
/// input relation as a view over <c>read_parquet(...)</c> — a local
/// file path or an <c>s3://</c> URI — verifies the SQL's described
/// result schema against the declared output schema, and writes the
/// result with <c>COPY (...) TO ... (FORMAT PARQUET)</c>, again local
/// or <c>s3://</c>. Rows never enter the .NET runtime — the only
/// reader loop below walks <c>DESCRIBE</c> output, which is one
/// metadata row per <em>column</em>, never per data row — and S3
/// objects never buffer in the CLR either: the engine's own
/// <c>httpfs</c> extension streams them natively.
/// </summary>
/// <remarks>
/// <para>
/// A fresh database per transform keeps transforms hermetic (no view
/// name collisions, no state bleed between steps) at negligible cost —
/// DuckDB's in-memory startup is microseconds against transform bodies
/// that process millions of rows. The same freshness scopes S3
/// credentials: each <c>s3://</c> endpoint's access handoff becomes a
/// <em>temporary</em> DuckDB secret <c>SCOPE</c>d to exactly that
/// object, living only inside the transform's private database — never
/// persisted, never logged, gone when the connection closes.
/// </para>
/// <para>
/// Schema verification runs <em>before</em> the <c>COPY</c>, so a
/// mismatched transform never writes a partial or mis-shaped output
/// file. <c>DESCRIBE</c> only binds the query — no input data is read
/// to answer it (for an <c>s3://</c> input, binding fetches only the
/// Parquet footer).
/// </para>
/// </remarks>
public sealed class InProcessDuckDbEngine : IDuckDbEngine
{
  /// <summary>
  /// The embedded library's version, probed once per process — every
  /// <see cref="InProcessDuckDbEngine"/> instance shares the same
  /// native library, so the probe (an in-memory connection open, a
  /// microseconds-scale cost paid once) never repeats.
  /// </summary>
  private static readonly Lazy<string> ProbedLibraryVersion = new(ProbeLibraryVersion);

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
  public string EngineVersion => ProbedLibraryVersion.Value;

  /// <summary>
  /// Read the embedded library's version from a throwaway in-memory
  /// connection (<c>DuckDBConnection.ServerVersion</c>, backed by
  /// <c>duckdb_library_version()</c>). If the native library can't
  /// load, return a per-process-unique sentinel instead of throwing:
  /// this property is consulted during cache-plan pre-flight, where an
  /// exception would abort planning untyped, while the unique sentinel
  /// merely keeps every transform permanently stale — and the real
  /// failure then surfaces as a typed error value the moment a
  /// transform executes. An unknown version must never produce a cache
  /// hit; the sentinel guarantees it can't.
  /// </summary>
  private static string ProbeLibraryVersion()
  {
    try
    {
      using var connection = new DuckDBConnection("Data Source=:memory:");
      connection.Open();
      return connection.ServerVersion;
    }
    catch
    {
      return $"unavailable:{Guid.NewGuid():N}";
    }
  }

  /// <inheritdoc/>
  public FlowIO<DuckDbTransformResult> ExecuteTransform(DuckDbTransformRequest request)
  {
    if (request is null) throw new ArgumentNullException(nameof(request));

    return FlowIO.LiftAsync(
        ct => RunTransformAsync(request, ct),
        source: $"DuckDbEngine.ExecuteTransform[{request.StepLabel}]"
      )
      .MapError(error => Translate(error, request));
  }

  // ── Transform body ──────────────────────────────────────────────────────

  private async Task<DuckDbTransformResult> RunTransformAsync(
    DuckDbTransformRequest request,
    CancellationToken ct
  )
  {
    var sql = TrimTerminator(request.Sql);
    var remoteEndpoints = CollectS3Endpoints(request);

    using var connection = new DuckDBConnection("Data Source=:memory:");
    await connection.OpenAsync(ct).ConfigureAwait(false);

    await ApplyEngineSettingsAsync(connection, ct).ConfigureAwait(false);

    if (remoteEndpoints.Count > 0)
    {
      await EnsureHttpfsAsync(connection, ct).ConfigureAwait(false);

      // One temporary secret per distinct s3 object, SCOPEd to exactly that
      // object's URI, so endpoints carrying different credentials never
      // resolve each other's handoff. Temporary secrets live only inside
      // this transform's private in-memory database.
      foreach (var secret in DuckDbS3SecretSql.Plan(remoteEndpoints))
      {
        await ExecuteAsync(connection, secret.Sql, ct).ConfigureAwait(false);
      }
    }

    // Bind each input as a view over its Parquet bytes. The view is a
    // pure query definition — nothing is read until the transform runs.
    foreach (var relation in request.Relations)
    {
      await ExecuteAsync(
        connection,
        $"CREATE VIEW {QuoteIdentifier(relation.Name)} AS "
        + $"SELECT * FROM read_parquet({QuoteLiteral(ReadTarget(relation))})",
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

    // Execute the transform and write the result inside the engine — to a
    // local file (parents created) or straight to the s3 object (httpfs
    // uploads engine-side; the object never buffers in the CLR).
    var copyTarget = request.OutputLocation.Match(
      onLocalFile: local =>
      {
        var fullPath = Path.GetFullPath(local.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        return fullPath;
      },
      onRemoteUri: remote => DuckDbS3SecretSql.ScopeFor(remote.Uri)
    );
    var rowsCopied = await CopyToParquetAsync(
      connection, sql, copyTarget, request.Options, ct
    ).ConfigureAwait(false);

    return new DuckDbTransformResult(rowsCopied, resultColumns);
  }

  // ── S3 endpoint handling ────────────────────────────────────────────────

  /// <summary>
  /// Every endpoint (inputs and output) whose bytes live behind a remote
  /// URI, validated to the one remote scheme the engine reaches:
  /// <c>s3://</c>. Any other scheme fails with the typed
  /// <c>RemoteBytesUnsupported</c> value — the step already rejects these
  /// with the item's own label; this is the engine's defensive check for
  /// directly-constructed requests.
  /// </summary>
  private static IReadOnlyList<ByteLocation.RemoteUri> CollectS3Endpoints(
    DuckDbTransformRequest request
  )
  {
    var endpoints = new List<ByteLocation.RemoteUri>();

    foreach (var relation in request.Relations)
    {
      if (relation.Location is ByteLocation.RemoteUri remote)
      {
        RequireS3(relation.Name, remote.Uri);
        endpoints.Add(remote);
      }
    }
    if (request.OutputLocation is ByteLocation.RemoteUri remoteOutput)
    {
      RequireS3($"{request.StepLabel} (output)", remoteOutput.Uri);
      endpoints.Add(remoteOutput);
    }

    return endpoints;
  }

  private static void RequireS3(string endpointLabel, Uri uri)
  {
    if (!string.Equals(uri.Scheme, "s3", StringComparison.OrdinalIgnoreCase))
    {
      throw new RemoteEndpointUnsupportedException(endpointLabel, uri);
    }
  }

  /// <summary>The literal <c>read_parquet</c> receives: a local path or the <c>s3://</c> URI.</summary>
  private static string ReadTarget(DuckDbBoundRelation relation) =>
    relation.Location.Match(
      onLocalFile: local => local.Path,
      onRemoteUri: remote => DuckDbS3SecretSql.ScopeFor(remote.Uri)
    );

  /// <summary>
  /// Make the <c>httpfs</c> extension available on this connection. The
  /// bundled engine does not statically link it, so: try <c>LOAD</c>
  /// (succeeds offline when the extension is already installed in the
  /// extension directory); if that fails and downloads are allowed, run
  /// the one-time <c>INSTALL httpfs</c> (network) and <c>LOAD</c> again.
  /// Anything else is the typed <c>FTDDB4003</c> failure — never a silent
  /// retry, never an implicit download when downloads are disabled.
  /// </summary>
  private async Task EnsureHttpfsAsync(DuckDBConnection connection, CancellationToken ct)
  {
    if (!_options.AllowExtensionDownload)
    {
      // Belt and braces: DuckDB's own autoinstall must not sneak a download
      // during query execution when the host forbids one.
      await ExecuteAsync(connection, "SET autoinstall_known_extensions=false", ct)
        .ConfigureAwait(false);
    }

    try
    {
      await ExecuteAsync(connection, "LOAD httpfs", ct).ConfigureAwait(false);
      return;
    }
    catch (DuckDBException loadFailure)
    {
      if (!_options.AllowExtensionDownload)
      {
        throw new HttpfsUnavailableException(
          $"httpfs is not installed locally and AllowExtensionDownload is false. "
          + $"DuckDB said: {loadFailure.Message}. Pre-provision httpfs.duckdb_extension "
          + "into the engine's extension directory (DuckDbEngineOptions.ExtensionDirectory, "
          + "default ~/.duckdb) by running INSTALL httpfs once on a networked machine with "
          + "the same DuckDB version and platform, or enable AllowExtensionDownload."
        );
      }
    }

    try
    {
      await ExecuteAsync(connection, "INSTALL httpfs", ct).ConfigureAwait(false);
      await ExecuteAsync(connection, "LOAD httpfs", ct).ConfigureAwait(false);
    }
    catch (DuckDBException installFailure)
    {
      throw new HttpfsUnavailableException(
        $"httpfs is not installed locally and the one-time INSTALL httpfs download "
        + $"failed. DuckDB said: {installFailure.Message}. This host likely has no "
        + "network path to DuckDB's extension repository — pre-provision "
        + "httpfs.duckdb_extension into the engine's extension directory "
        + "(DuckDbEngineOptions.ExtensionDirectory, default ~/.duckdb) instead."
      );
    }
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
    if (_options.ExtensionDirectory is { } extensionDirectory)
    {
      await ExecuteAsync(
        connection, $"SET extension_directory={QuoteLiteral(extensionDirectory)}", ct
      ).ConfigureAwait(false);
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
    string copyTarget,
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
      $"COPY ({sql}) TO {QuoteLiteral(copyTarget)} ({copyOptions})";

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
  /// adapters use), a <see cref="DuckDBException"/> becomes the
  /// extension's <see cref="DuckDbRuntimeError.EngineFailed"/> — with
  /// any S3 credential material scrubbed out of the detail first, since
  /// DuckDB error text echoes the offending SQL — and the engine's own
  /// httpfs / remote-scheme failures become their typed values. Other
  /// errors pass through unchanged.
  /// </summary>
  private static RuntimeError Translate(RuntimeError error, DuckDbTransformRequest request) =>
    error switch
    {
      RuntimeError.External { Cause: SchemaMismatchException smx } external =>
        new RuntimeError.SchemaMismatch(
          Source: external.Source,
          Detail: smx.Message,
          InnerExceptionInfo: smx.InnerException?.ToString()
        ),
      RuntimeError.External { Cause: RemoteEndpointUnsupportedException reu } =>
        new RuntimeError.ExtensionError(
          new DuckDbRuntimeError.RemoteBytesUnsupported(reu.EndpointLabel, reu.Uri)
        ),
      RuntimeError.External { Cause: HttpfsUnavailableException hue } =>
        new RuntimeError.ExtensionError(
          new DuckDbRuntimeError.HttpfsUnavailable(request.StepLabel, hue.Message)
        ),
      RuntimeError.External { Cause: DuckDBException dde } =>
        new RuntimeError.ExtensionError(
          new DuckDbRuntimeError.EngineFailed(request.StepLabel, RedactForRequest(dde.Message, request))
        ),
      _ => error,
    };

  /// <summary>
  /// Scrub the request's S3 credential material out of an engine
  /// message. DuckDB parser/binder errors echo the SQL they rejected —
  /// for a failed <c>CREATE SECRET</c> that would be the credentials
  /// themselves — so every engine-message-bearing detail passes through
  /// here before becoming an error value.
  /// </summary>
  private static string RedactForRequest(string message, DuckDbTransformRequest request)
  {
    var remoteEndpoints = new List<ByteLocation.RemoteUri>();
    foreach (var relation in request.Relations)
    {
      if (relation.Location is ByteLocation.RemoteUri remote) remoteEndpoints.Add(remote);
    }
    if (request.OutputLocation is ByteLocation.RemoteUri remoteOutput)
    {
      remoteEndpoints.Add(remoteOutput);
    }
    if (remoteEndpoints.Count == 0) return message;

    return DuckDbS3SecretSql.Redact(
      message, DuckDbS3SecretSql.SensitiveValues(remoteEndpoints));
  }

  // ── SQL text assembly ───────────────────────────────────────────────────
  // Shared with the hermetic pre-flight check via DuckDbSql, so the SQL
  // pre-flight binds is byte-identical to the SQL this engine executes.

  private static string TrimTerminator(string sql) => DuckDbSql.TrimTerminator(sql);

  private static string QuoteIdentifier(string identifier) =>
    DuckDbSql.QuoteIdentifier(identifier);

  private static string QuoteLiteral(string value) => DuckDbSql.QuoteLiteral(value);

  private static string CompressionName(DuckDbParquetCompression compression) =>
    compression switch
    {
      DuckDbParquetCompression.Snappy => "SNAPPY",
      DuckDbParquetCompression.Zstd => "ZSTD",
      DuckDbParquetCompression.Gzip => "GZIP",
      DuckDbParquetCompression.Uncompressed => "UNCOMPRESSED",
      _ => throw new ArgumentOutOfRangeException(nameof(compression), compression, null),
    };

  // ── Engine-internal failure carriers ────────────────────────────────────
  // Thrown inside RunTransformAsync, translated to their typed error
  // values at the FlowIO boundary — never observable outside this class.

  /// <summary>An endpoint's remote URI has a scheme the engine can't reach (only <c>s3://</c> is supported).</summary>
  private sealed class RemoteEndpointUnsupportedException : Exception
  {
    public RemoteEndpointUnsupportedException(string endpointLabel, Uri uri)
      : base($"Endpoint '{endpointLabel}' has unsupported remote scheme '{uri.Scheme}'.")
    {
      EndpointLabel = endpointLabel;
      Uri = uri;
    }

    public string EndpointLabel { get; }
    public Uri Uri { get; }
  }

  /// <summary>The httpfs extension an <c>s3://</c> endpoint needs could not be loaded.</summary>
  private sealed class HttpfsUnavailableException : Exception
  {
    public HttpfsUnavailableException(string message) : base(message) { }
  }
}
