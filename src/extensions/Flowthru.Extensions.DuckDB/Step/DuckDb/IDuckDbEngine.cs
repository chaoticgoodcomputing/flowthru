using Flowthru.Prelude;

namespace Flowthru.Step.DuckDb;

/// <summary>
/// The embedded DuckDB engine every DuckDB transform step executes
/// through. Registered as a singleton by <c>UseDuckDb()</c> and passed
/// to <c>AddDuckDbTransform</c> at flow wire-up (typically via
/// <c>RegisterFlow&lt;Catalog, IDuckDbEngine&gt;</c>), the same shape as
/// the Python extension's executor.
/// </summary>
/// <remarks>
/// <para>
/// The engine is the transform steps' shared conflict resource: each
/// step declares it as a service dependency, and the scheduler holds
/// concurrent transforms to <see cref="MaxConcurrency"/> so a flow's
/// peak engine memory/disk stays bounded. Its resolved profile is
/// cache-neutral — which engine <em>instance</em> ran a transform adds
/// no caching information. Which engine <em>version</em> ran it does:
/// <see cref="EngineVersion"/> feeds every transform step's declared
/// cache identity (see <c>DuckDbTransformStep</c>).
/// </para>
/// </remarks>
public interface IDuckDbEngine
{
  /// <summary>
  /// Maximum number of transforms this engine supports running
  /// concurrently. The scheduler gates DuckDB steps on this capacity.
  /// </summary>
  int MaxConcurrency { get; }

  /// <summary>
  /// The DuckDB library version this engine executes transforms with
  /// (e.g. <c>"v1.5.3"</c>). Folded into every transform step's cache
  /// identity: results computed under one engine version must never be
  /// served as cached output under another, because the engine's query
  /// semantics and Parquet writer can change between versions. Must be
  /// deterministic and stable for the life of the process.
  /// </summary>
  string EngineVersion { get; }

  /// <summary>
  /// Execute one engine-delegated transform: bind each requested
  /// relation over its Parquet file, verify the SQL's result schema
  /// against the request's expected columns, and write the result
  /// straight to the output path as Parquet. Rows never enter the .NET
  /// runtime — the read, the transform, and the write all happen inside
  /// the engine.
  /// </summary>
  /// <remarks>
  /// Nothing runs until the returned effect runs. Failures surface as
  /// typed error values through the <see cref="FlowIO{A}"/> failure
  /// channel — a result-schema mismatch as
  /// <see cref="Flowthru.Validation.Runtime.RuntimeError.SchemaMismatch"/>,
  /// engine execution errors as
  /// <see cref="Flowthru.Validation.Runtime.DuckDb.DuckDbRuntimeError"/>
  /// wrapped in
  /// <see cref="Flowthru.Validation.Runtime.RuntimeError.ExtensionError"/>.
  /// Nothing throws.
  /// </remarks>
  FlowIO<DuckDbTransformResult> ExecuteTransform(DuckDbTransformRequest request);
}
