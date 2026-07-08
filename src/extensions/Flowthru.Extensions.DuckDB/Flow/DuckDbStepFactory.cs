using Flowthru.Data.Catalog;
using Flowthru.Step.DuckDb;

namespace Flowthru.Flow;

/// <summary>
/// <c>AddDuckDbTransform</c> extension methods on
/// <see cref="FlowBuilder"/> — wire a SQL transform between ordinary
/// Parquet catalog items, executed entirely inside the embedded DuckDB
/// engine.
/// </summary>
/// <remarks>
/// <para>
/// Use a DuckDB transform where the step must consume all of its input
/// before emitting output — global sorts, deduplication, aggregation,
/// joins — over data too large to hold comfortably in memory. Each
/// input binds to a SQL relation named after its item label (override
/// per input via <see cref="DuckDbInputRelation.From{TRow}"/>), and the
/// query's result is written straight to the output item:
/// </para>
/// <code>
/// flow.AddDuckDbTransform(
///   label: "sort_events",
///   input: catalog.Events,          // relation "Events"
///   output: catalog.SortedEvents,
///   sql: "SELECT * FROM Events ORDER BY Country, OccurredAt",
///   engine: engine);                // IDuckDbEngine from UseDuckDb()
/// </code>
/// <para>
/// The SQL is schema-validated in phases: at pre-flight the hermetic
/// check registered by <c>UseDuckDb()</c> binds it against empty
/// in-engine tables built from the declared input schemas and verifies
/// the result against the output item's declared schema (no real data
/// touched — failures name the step, relation binding, and offending
/// columns); at execution the same verification runs against the real
/// files before anything is written. Wiring problems — a
/// non-file-backed endpoint, duplicate relation names — fail here, at
/// wire-up, and a unit test can run the same check via
/// <c>flow.ValidateDuckDbTransforms()</c> or
/// <c>FUnitContext.Validate(step)</c>.
/// </para>
/// </remarks>
public static class DuckDbStepFactory
{
  /// <summary>
  /// Add a DuckDB transform with one input and one output. The input
  /// binds to a relation named after its item label.
  /// </summary>
  /// <typeparam name="TIn">Input row type — must match the input item's element type.</typeparam>
  /// <typeparam name="TOut">Output row type — the schema the SQL result is verified against.</typeparam>
  /// <param name="builder">Flow builder.</param>
  /// <param name="label">Unique step label within the flow.</param>
  /// <param name="input">
  /// The input item — a byte-addressable Parquet item; its label is the
  /// SQL relation name.
  /// </param>
  /// <param name="output">The output item the result is written to.</param>
  /// <param name="sql">The transform body — a single SQL query over the input relation.</param>
  /// <param name="engine">The DuckDB engine (registered by <c>UseDuckDb()</c>).</param>
  /// <param name="options">Optional output-write tuning (compression, row-group size).</param>
  public static FlowBuilder AddDuckDbTransform<TIn, TOut>(
    this FlowBuilder builder,
    string label,
    IItem<IEnumerable<TIn>> input,
    IItem<IEnumerable<TOut>> output,
    string sql,
    IDuckDbEngine engine,
    DuckDbTransformOptions? options = null
  )
    where TIn : notnull
    where TOut : notnull
  {
    if (input is null) throw new ArgumentNullException(nameof(input));

    return builder.AddDuckDbTransform(
      label,
      new[] { DuckDbInputRelation.From(input) },
      output,
      sql,
      engine,
      options
    );
  }

  /// <summary>
  /// Add a DuckDB transform over any number of input relations — the
  /// multi-input shape for joins and unions. Bind each input with
  /// <see cref="DuckDbInputRelation.From{TRow}"/>, naming it explicitly
  /// where the item label doesn't read well in SQL.
  /// </summary>
  /// <typeparam name="TOut">Output row type — the schema the SQL result is verified against.</typeparam>
  /// <param name="builder">Flow builder.</param>
  /// <param name="label">Unique step label within the flow.</param>
  /// <param name="inputs">The input relation bindings; names must be distinct.</param>
  /// <param name="output">The output item the result is written to.</param>
  /// <param name="sql">The transform body — a single SQL query over the bound relations.</param>
  /// <param name="engine">The DuckDB engine (registered by <c>UseDuckDb()</c>).</param>
  /// <param name="options">Optional output-write tuning (compression, row-group size).</param>
  public static FlowBuilder AddDuckDbTransform<TOut>(
    this FlowBuilder builder,
    string label,
    IReadOnlyList<DuckDbInputRelation> inputs,
    IItem<IEnumerable<TOut>> output,
    string sql,
    IDuckDbEngine engine,
    DuckDbTransformOptions? options = null
  )
    where TOut : notnull
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (string.IsNullOrWhiteSpace(label))
      throw new ArgumentException("Label cannot be null or whitespace.", nameof(label));

    var step = new DuckDbTransformStep<TOut>(
      label: label,
      sql: sql,
      inputs: inputs,
      output: output,
      engine: engine,
      options: options
    );

    return builder.Add(step);
  }
}
