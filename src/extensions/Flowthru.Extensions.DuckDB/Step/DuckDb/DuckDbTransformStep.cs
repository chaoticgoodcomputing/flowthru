using Flowthru.Caching;
using Flowthru.Data.Catalog;
using Flowthru.Data.Schema.Mapping;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
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
/// <strong>Caching.</strong> The step declares itself uncacheable (see
/// <see cref="DeclaredUncacheableReason"/>): its behaviour lives in the
/// SQL text, which the cache identity doesn't fingerprint yet, and a
/// cached result that survived a query edit would be silently stale.
/// The opt-out is loud — it surfaces wherever cache decisions are
/// reported.
/// </para>
/// </remarks>
public sealed class DuckDbTransformStep<TOut> : IStepNode
  where TOut : notnull
{
  /// <summary>
  /// The shared engine every DuckDB transform executes through.
  /// Declared as a service dependency so the scheduler can gate
  /// concurrent transforms on the engine's capacity (each transform may
  /// use the engine's full memory budget) — see
  /// <c>DuckDbEngineProfileContributor</c>. Its resolved profile is
  /// cache-neutral; the step's cache opt-out comes from
  /// <see cref="DeclaredUncacheableReason"/> instead.
  /// </summary>
  internal static readonly ServiceDependency EngineDependency =
    ServiceDependency.Of<IDuckDbEngine>();

  private readonly IDuckDbEngine _engine;
  private readonly IReadOnlyList<DuckDbInputRelation> _relations;
  private readonly FlowIO<ByteLocation> _outputLocation;
  private readonly IReadOnlyList<DuckDbExpectedColumn> _expectedColumns;
  private readonly DuckDbTransformOptions _options;

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
    _expectedColumns = BuildExpectedColumns();

    Inputs = inputs.Select(r => r.Item).ToArray();
    Outputs = new IItem[] { output };
    ServiceDependencies = new[] { EngineDependency };
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
  /// The step's behaviour is the SQL text — wire-up data, not compiled
  /// step code — and the cache identity doesn't fingerprint it yet.
  /// Caching under an identity blind to the query would serve stale
  /// output after any edit, so the step opts out, loudly.
  /// </remarks>
  public StepUncacheableReason? DeclaredUncacheableReason { get; } =
    new StepUncacheableReason.DeclaredByStep(
      "DuckDB transform: the SQL text is wire-up data that isn't part of the step's "
      + "cache identity, so results are never cached — an edited query must never be "
      + "served stale output"
    );

  /// <inheritdoc/>
  public FlowIO<ValidationResult> Validate() =>
    FlowIO.Pure(ValidationResult.Success());

  /// <inheritdoc/>
  /// <remarks>
  /// Resolves every endpoint's byte location, then hands the whole
  /// transform to the engine. No item <c>Load()</c>/<c>Save()</c> is
  /// involved — the payload bytes move engine-side only.
  /// </remarks>
  public FlowIO<FlowUnit> Execute() =>
    from relations in ResolveRelations()
    from outputPath in ResolveLocalPath(Output.Label, _outputLocation)
    from result in _engine.ExecuteTransform(new DuckDbTransformRequest(
      StepLabel: Label,
      Relations: relations,
      Sql: Sql,
      OutputPath: outputPath,
      ExpectedColumns: _expectedColumns,
      Options: _options
    ))
    select FlowUnit.Default;

  // ── Byte-location resolution ────────────────────────────────────────────

  private FlowIO<IReadOnlyList<DuckDbBoundRelation>> ResolveRelations()
  {
    var bound = FlowIO.Pure(new List<DuckDbBoundRelation>(_relations.Count));
    foreach (var relation in _relations)
    {
      var current = relation;
      bound = bound.Bind(list =>
        ResolveLocalPath(current.Item.Label, current.Location)
          .Map(path =>
          {
            list.Add(new DuckDbBoundRelation(current.RelationName, path));
            return list;
          })
      );
    }
    return bound.Map(list => (IReadOnlyList<DuckDbBoundRelation>)list);
  }

  /// <summary>
  /// Collapse a resolved <see cref="ByteLocation"/> to a local path.
  /// Remote locations fail with the typed
  /// <see cref="DuckDbRuntimeError.RemoteBytesUnsupported"/> value —
  /// local files are the only location this transform reaches today.
  /// </summary>
  private static FlowIO<string> ResolveLocalPath(
    string itemLabel,
    FlowIO<ByteLocation> location
  ) =>
    location.Bind(resolved => resolved.Match(
      onLocalFile: local => FlowIO.Pure(local.Path),
      onRemoteUri: remote => FlowIO.Fail<string>(
        new RuntimeError.ExtensionError(
          new DuckDbRuntimeError.RemoteBytesUnsupported(itemLabel, remote.Uri)
        ))
    ));

  // ── Declared output schema ──────────────────────────────────────────────

  /// <summary>
  /// Project <typeparamref name="TOut"/>'s declared schema into the
  /// column set the engine verifies the SQL result against — property
  /// names (honouring <c>[SerializedLabel]</c>) and round-trip CLR
  /// types, with enums unwrapped to their underlying integer type
  /// (matching how Parquet stores them).
  /// </summary>
  private static IReadOnlyList<DuckDbExpectedColumn> BuildExpectedColumns()
  {
    var plan = PropertyMappingPlanner.Build<TOut>();
    var columns = new List<DuckDbExpectedColumn>(plan.Bindings.Count);

    foreach (var binding in plan.Bindings)
    {
      var clrType = binding.Kind switch
      {
        PropertyKind.Primitive => binding.EffectiveType,
        PropertyKind.Enum => Enum.GetUnderlyingType(binding.EffectiveType),
        _ => throw new ArgumentException(
          $"Output schema property '{typeof(TOut).Name}.{binding.Property.Name}' is "
          + $"classified as {binding.Kind}, which the DuckDB transform's schema "
          + "verification doesn't support yet — it covers primitive and enum columns. "
          + "Widen the property to a primitive, or transform into an intermediate "
          + "schema and map it in an ordinary step."
        ),
      };

      if (!Internal.DuckDbTypeMap.IsSupported(clrType))
      {
        throw new ArgumentException(
          $"Output schema property '{typeof(TOut).Name}.{binding.Property.Name}' has type "
          + $"{clrType.Name}, which the DuckDB transform's schema verification doesn't "
          + "know how to check. Use a supported primitive type, or map the result in an "
          + "ordinary step."
        );
      }

      columns.Add(new DuckDbExpectedColumn(binding.FieldName, clrType, binding.IsNullable));
    }

    return columns;
  }
}
