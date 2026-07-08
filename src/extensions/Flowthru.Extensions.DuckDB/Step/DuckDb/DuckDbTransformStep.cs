using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Validation.PreFlight.DuckDb;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.DuckDb;

namespace Flowthru.Step.DuckDb;

/// <summary>
/// A flow step whose entire body is SQL executed inside the embedded
/// DuckDB engine. Wired between ordinary row-sequence Parquet items —
/// it participates in the DAG, scheduling, and pre-flight like any
/// other step — but at runtime the rows never enter the .NET runtime:
/// the engine reads the input files, runs the SQL, and writes the
/// output file directly.
/// </summary>
/// <typeparam name="TOut">
/// The output item's row type. Its declared schema is what the SQL's
/// result schema is verified against before anything is written.
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>When to use it.</strong> Transforms that must see all their
/// input before emitting output — global sorts, deduplication,
/// aggregation, joins — pay a heavy per-row cost as in-memory LINQ once
/// data is large. Delegating the transform to the engine keeps memory
/// bounded and typically runs an order of magnitude faster. Row-at-a-
/// time logic (map, filter with per-row context) belongs in ordinary
/// steps, where it composes with the rest of your C#.
/// </para>
/// <para>
/// <strong>Pure description.</strong> Construction performs no IO — it
/// captures the SQL, the endpoint bindings, and the engine reference.
/// Endpoint byte locations resolve when the step executes; failures
/// surface as typed error values through the step result, never as
/// thrown exceptions.
/// </para>
/// <para>
/// <strong>Caching.</strong> First-class cacheable. The step's
/// behaviour lives in wire-up data rather than compiled code, so it
/// declares that data into its cache identity (see
/// <see cref="DeclaredCacheIdentity"/>): a hash of the exact SQL text,
/// the engine version, the relation-name bindings, and the
/// output-affecting transform options. Unchanged SQL over unchanged
/// inputs with the output present skips like any other cached step;
/// editing the query, bumping the engine, or changing how the output
/// file is written each invalidates.
/// </para>
/// </remarks>
public sealed class DuckDbTransformStep<TOut> : IStepNode, IDuckDbTransformDescriptor
  where TOut : notnull
{
  /// <summary>
  /// The shared engine every DuckDB transform executes through.
  /// Declared as a service dependency so the scheduler can gate
  /// concurrent transforms on the engine's capacity (each transform may
  /// use the engine's full memory budget) — see
  /// <c>DuckDbEngineProfileContributor</c>. Its resolved profile is
  /// cache-neutral: which engine <em>instance</em> runs a transform
  /// adds no caching information, while the engine <em>version</em>
  /// enters the cache key through
  /// <see cref="DeclaredCacheIdentity"/>.
  /// </summary>
  internal static readonly ServiceDependency EngineDependency =
    ServiceDependency.Of<IDuckDbEngine>();

  /// <summary>
  /// Build-time identity of the transform <em>machinery</em> — the
  /// extension assembly version, which is what compiles this step's
  /// relation binding, schema verification, and COPY assembly. The
  /// wire-up data the machinery executes (SQL, engine version, output
  /// options) is deliberately not here: it lives in
  /// <see cref="DeclaredCacheIdentity"/>, the seam for identity that
  /// isn't compiled step code.
  /// </summary>
  private static readonly string MachineryCodeVersion =
    "flowthru-duckdb-transform:"
    + (typeof(DuckDbTransformStep<>).Assembly.GetName().Version?.ToString() ?? "0.0.0.0");

  private readonly IDuckDbEngine _engine;
  private readonly IReadOnlyList<DuckDbInputRelation> _relations;
  private readonly FlowIO<ByteLocation> _outputLocation;
  private readonly IReadOnlyList<DuckDbExpectedColumn> _expectedColumns;
  private readonly DuckDbTransformOptions _options;
  private readonly Lazy<string> _cacheIdentity;

  /// <summary>
  /// Construct a typed DuckDB transform description. No IO is performed;
  /// schema-shape and wiring problems (duplicate relation names, an
  /// output schema the verifier can't check) fail here, at wire-up.
  /// </summary>
  public DuckDbTransformStep(
    string label,
    string sql,
    IReadOnlyList<DuckDbInputRelation> inputs,
    IItem<IEnumerable<TOut>> output,
    IDuckDbEngine engine,
    DuckDbTransformOptions? options = null
  )
  {
    Label = label ?? throw new ArgumentNullException(nameof(label));
    Sql = sql ?? throw new ArgumentNullException(nameof(sql));
    if (inputs is null) throw new ArgumentNullException(nameof(inputs));
    if (output is null) throw new ArgumentNullException(nameof(output));
    _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    _options = options ?? DuckDbTransformOptions.Default;

    if (string.IsNullOrWhiteSpace(sql))
    {
      throw new ArgumentException("Transform SQL cannot be empty.", nameof(sql));
    }
    if (inputs.Count == 0)
    {
      throw new ArgumentException(
        "A DuckDB transform needs at least one input relation.", nameof(inputs)
      );
    }

    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var relation in inputs)
    {
      if (!names.Add(relation.RelationName))
      {
        throw new ArgumentException(
          $"Relation name '{relation.RelationName}' is bound more than once. Give each "
          + "input a distinct name via DuckDbInputRelation.From(item, relationName).",
          nameof(inputs)
        );
      }
    }

    _relations = inputs;
    Output = output;
    // Validates the output is byte-addressable now (throws for memory/
    // database-backed items); resolves the location when Execute runs.
    _outputLocation = output.LocateBytes();

    // Project TOut's declared schema into the columns the SQL result is
    // verified against. An unmappable output schema is a wiring bug at
    // the author's call site, so it throws here rather than surfacing
    // later as a confusing runtime mismatch.
    var outputProjection = DuckDbDeclaredSchema.Project<TOut>();
    _expectedColumns = outputProjection.Columns
      ?? throw new ArgumentException(
        $"Output schema for this DuckDB transform can't be verified: "
        + outputProjection.Problem,
        nameof(output)
      );

    Inputs = inputs.Select(r => r.Item).ToArray();
    Outputs = new IItem[] { output };
    ServiceDependencies = new[] { EngineDependency };
    _cacheIdentity = new Lazy<string>(ComposeCacheIdentity);
  }

  /// <inheritdoc/>
  public string Label { get; }

  /// <inheritdoc/>
  public string FlowLabel { get; private set; } = string.Empty;

  /// <inheritdoc/>
  public void OnAddedToFlow(string flowLabel)
  {
    if (string.IsNullOrEmpty(FlowLabel))
      FlowLabel = flowLabel;
  }

  /// <inheritdoc/>
  public NodeTraits Traits { get; } = new();

  /// <summary>The transform body — the SQL the engine executes.</summary>
  public string Sql { get; }

  /// <summary>The input relation bindings, in wire-up order.</summary>
  public IReadOnlyList<DuckDbInputRelation> InputRelations => _relations;

  /// <summary>The output item the result is written to.</summary>
  public IItem<IEnumerable<TOut>> Output { get; }

  /// <inheritdoc/>
  public IReadOnlyList<IItem> Inputs { get; }

  /// <inheritdoc/>
  public IReadOnlyList<IItem> Outputs { get; }

  /// <inheritdoc/>
  public IReadOnlyList<ServiceDependency> ServiceDependencies { get; }

  /// <inheritdoc/>
  /// <remarks>
  /// Always <c>"sql"</c>, so DAG renderers can tag the step the same
  /// way Python steps are tagged — without referencing this concrete
  /// type.
  /// </remarks>
  public string? SourceLanguage => "sql";

  /// <inheritdoc/>
  /// <remarks>
  /// The extension machinery's identity — see
  /// <see cref="MachineryCodeVersion"/>. Non-null is the promise that
  /// two runs with the same inputs <em>and the same
  /// <see cref="DeclaredCacheIdentity"/></em> produce equivalent
  /// outputs, which is exactly the determinism a relational engine
  /// offers for a fixed query, engine version, and write options.
  /// </remarks>
  public string? CodeVersion => MachineryCodeVersion;

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// The wire-up data that decides this transform's output, reduced to
  /// a stable token:
  /// </para>
  /// <list type="bullet">
  /// <item>SHA-256 of the <em>exact</em> SQL text — no normalization,
  /// so any edit (even whitespace) invalidates rather than risking a
  /// stale hit on a semantic change a normalizer misjudged.</item>
  /// <item>The engine version — query semantics and the engine's
  /// Parquet writer can change between DuckDB releases.</item>
  /// <item>The relation-name → item-label bindings (sorted by relation
  /// name) — rebinding the same items to different names changes what
  /// the same SQL text reads, which input fingerprints alone can't
  /// see.</item>
  /// <item>The output-affecting transform options — compression codec
  /// and row-group size both change the produced file's bytes.</item>
  /// </list>
  /// <para>
  /// Evaluated lazily (the engine version probe is deferred until the
  /// cache planner first asks) and cached per step instance.
  /// </para>
  /// </remarks>
  public string? DeclaredCacheIdentity => _cacheIdentity.Value;

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// Runs the hermetic SQL schema check: empty in-memory tables are
  /// built from the <em>declared</em> input record schemas (named per
  /// this step's relation bindings), the SQL is <c>DESCRIBE</c>d against
  /// them — binding without executing — and the described result schema
  /// is verified against <typeparamref name="TOut"/>'s declared schema.
  /// Nothing outside the process is reached and no real data is read.
  /// </para>
  /// <para>
  /// This is the design-time surface: run it from a unit test (e.g.
  /// <c>FUnitContext.Validate(step)</c>, or
  /// <c>flow.ValidateDuckDbTransforms()</c> over a built flow) and a
  /// schema-breaking SQL edit fails the test with the same diagnostics
  /// pre-flight would report. The engine never calls it on the run
  /// path — the happy path pays only the pre-flight check itself.
  /// </para>
  /// </remarks>
  public FlowIO<ValidationResult> Validate() =>
    FlowIO.LiftAsync(
      async ct =>
      {
        var failures = await DuckDbSqlSchemaCheck.RunAsync(this, ct).ConfigureAwait(false);
        return failures.Count == 0
          ? ValidationResult.Success()
          : new ValidationResult(
              failures.Select(f => DuckDbSqlSchemaCheck.ToValidationError(Label, f))
            );
      },
      source: $"DuckDbTransformStep[{Label}].Validate"
    );

  // ── Pre-flight descriptor (IDuckDbTransformDescriptor) ─────────────────

  /// <inheritdoc/>
  IItem IDuckDbTransformDescriptor.OutputItem => Output;

  /// <inheritdoc/>
  string IDuckDbTransformDescriptor.OutputSchemaName => typeof(TOut).Name;

  /// <inheritdoc/>
  IReadOnlyList<DuckDbExpectedColumn> IDuckDbTransformDescriptor.ExpectedOutputColumns =>
    _expectedColumns;

  /// <inheritdoc/>
  /// <remarks>
  /// Resolves every endpoint's byte location, then hands the whole
  /// transform to the engine. No item <c>Load()</c>/<c>Save()</c> is
  /// involved — the payload bytes move engine-side only, whether they
  /// live in local files or behind <c>s3://</c> URIs.
  /// </remarks>
  public FlowIO<FlowUnit> Execute() =>
    from relations in ResolveRelations()
    from outputLocation in ResolveEndpoint(Output.Label, _outputLocation)
    from result in _engine.ExecuteTransform(new DuckDbTransformRequest(
      StepLabel: Label,
      Relations: relations,
      Sql: Sql,
      OutputLocation: outputLocation,
      ExpectedColumns: _expectedColumns,
      Options: _options
    ))
    select FlowUnit.Default;

  // ── Cache identity ──────────────────────────────────────────────────────

  /// <summary>
  /// Assemble the declared cache identity from the output-affecting
  /// wire-up data. See <see cref="DeclaredCacheIdentity"/> for what
  /// goes in and why; segments are pipe-delimited and the SQL enters as
  /// a SHA-256 over its exact UTF-8 bytes.
  /// </summary>
  private string ComposeCacheIdentity()
  {
    var sqlHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Sql)));
    var bindings = string.Join(",", _relations
      .OrderBy(r => r.RelationName, StringComparer.Ordinal)
      .Select(r => $"{r.RelationName}={r.Item.Label}"));
    var rowGroupSize = _options.RowGroupSize is { } size
      ? size.ToString(CultureInfo.InvariantCulture)
      : "engine-default";

    return $"duckdb|sql-sha256:{sqlHash}"
      + $"|engine:{_engine.EngineVersion}"
      + $"|relations:{bindings}"
      + $"|compression:{_options.Compression}"
      + $"|row-group-size:{rowGroupSize}";
  }

  // ── Byte-location resolution ────────────────────────────────────────────

  private FlowIO<IReadOnlyList<DuckDbBoundRelation>> ResolveRelations()
  {
    var bound = FlowIO.Pure(new List<DuckDbBoundRelation>(_relations.Count));
    foreach (var relation in _relations)
    {
      var current = relation;
      bound = bound.Bind(list =>
        ResolveEndpoint(current.Item.Label, current.Location)
          .Map(location =>
          {
            list.Add(new DuckDbBoundRelation(current.RelationName, location));
            return list;
          })
      );
    }
    return bound.Map(list => (IReadOnlyList<DuckDbBoundRelation>)list);
  }

  /// <summary>
  /// Validate a resolved <see cref="ByteLocation"/> as an engine
  /// endpoint: local files and <c>s3://</c> objects pass through (the
  /// engine reads both natively — S3 via <c>httpfs</c> plus a
  /// connection-scoped secret minted from the location's access
  /// handoff); any other remote scheme fails with the typed
  /// <see cref="DuckDbRuntimeError.RemoteBytesUnsupported"/> value,
  /// attributed to the item whose bytes live there.
  /// </summary>
  private static FlowIO<ByteLocation> ResolveEndpoint(
    string itemLabel,
    FlowIO<ByteLocation> location
  ) =>
    location.Bind(resolved => resolved.Match(
      onLocalFile: local => FlowIO.Pure<ByteLocation>(local),
      onRemoteUri: remote =>
        string.Equals(remote.Uri.Scheme, "s3", StringComparison.OrdinalIgnoreCase)
          ? FlowIO.Pure<ByteLocation>(remote)
          : FlowIO.Fail<ByteLocation>(
              new RuntimeError.ExtensionError(
                new DuckDbRuntimeError.RemoteBytesUnsupported(itemLabel, remote.Uri)
              ))
    ));

}
