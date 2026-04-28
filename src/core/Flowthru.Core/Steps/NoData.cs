namespace Flowthru.Core.Steps;

/// <summary>
/// Marker type representing "no meaningful data" for nodes with side-effects or data generation.
/// Used as input/output type when a step doesn't consume or produce meaningful data.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design rationale:</strong> NoData provides a type-safe way to represent nodes that:
/// </para>
/// <list type="bullet">
///   <item>Generate data without inputs (synthetic data, seeding)</item>
///   <item>Perform side-effects without outputs (validation, logging, diagnostics)</item>
/// </list>
/// <para>
/// Inspired by functional programming's "Unit" type but with naming closer to typical .NET usage.
/// </para>
/// <para>
/// <strong>Pipeline registration:</strong> Use <see cref="Item"/> in either input or output position
/// when wiring a step that has no meaningful data on that side. Each access yields a unique catalog
/// entry to avoid DAG conflicts.
/// </para>
/// <code>
/// pipeline.AddStep&lt;ValidationStep&gt;(
///     input: catalog.InputData,
///     output: NoData.Item   // side-effect-only step
/// );
///
/// pipeline.AddStep&lt;GenerateDataStep&gt;(
///     input: NoData.Item,   // no-input step
///     output: catalog.GeneratedData
/// );
/// </code>
/// <para>
/// Use <see cref="Value"/> when returning <c>NoData</c> from a step's transform, or
/// <see cref="Result"/> for the standard <c>Task&lt;IEnumerable&lt;NoData&gt;&gt;</c> wrapper.
/// </para>
/// </remarks>
public sealed class NoData
{
  private static int _uniqueIdCounter = 0;

  /// <summary>
  /// Singleton instance returned from step transformations that produce <c>NoData</c>.
  /// </summary>
  public static readonly NoData Value = new();

  /// <summary>
  /// Yields a unique null catalog entry for use in either input or output position when wiring
  /// a step. Each access returns a fresh instance with a unique key so the DAG can distinguish
  /// independent NoData edges.
  /// </summary>
  public static Data.IItem<NoData> Item =>
    Data.ItemFactory.Null<NoData>($"_nodata_{Interlocked.Increment(ref _uniqueIdCounter)}");

  /// <summary>
  /// Returns the standard <c>NoData</c> result for side-effect-only steps. Eliminates the
  /// verbose <c>Task.FromResult(Enumerable.Repeat(NoData.Value, 1))</c> boilerplate.
  /// </summary>
  /// <returns>Singleton collection containing <see cref="Value"/>.</returns>
  public static System.Threading.Tasks.Task<IEnumerable<NoData>> Result()
  {
    return System.Threading.Tasks.Task.FromResult(Enumerable.Repeat(Value, 1));
  }

  private NoData() { }
}
