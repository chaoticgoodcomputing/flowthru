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
/// cache-neutral — the engine's identity adds no caching information —
/// though v1 transform steps declare themselves uncacheable regardless
/// (see <c>DuckDbTransformStep</c>).
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
