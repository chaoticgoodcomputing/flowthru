using Flowthru.Validation.PreFlight;

namespace Flowthru.Validation.PreFlight.DuckDb;

/// <summary>
/// Closed sum of every typed pre-flight failure mode the DuckDB
/// extension's SQL schema validation can surface. Wraps into Core's
/// <see cref="PreFlightError.External"/> via the
/// <see cref="IExtensionPreFlightError"/> contract — consumers that want
/// DuckDB-aware diagnostics pattern-match on
/// <c>case PreFlightError.External(DuckDbPreFlightError ext) =&gt; ...</c>;
/// consumers that don't care still get
/// <see cref="IExtensionPreFlightError.Message"/> through the standard
/// pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Every case names the step label and the relation/output binding it
/// concerns — the cause (a contract disagreement between the SQL and a
/// declared schema), not the symptom a runtime failure would show.
/// </para>
/// <para>
/// Diagnostic codes live in the FTDDB30xx range (pre-flight sibling of
/// the FTDDB40xx runtime range):
/// <list type="bullet">
///   <item>FTDDB3001 — SQL failed to prepare against the declared input schemas</item>
///   <item>FTDDB3002 — SQL result schema doesn't satisfy the declared output schema</item>
///   <item>FTDDB3003 — an input's declared schema can't be modelled for checking</item>
/// </list>
/// </para>
/// </remarks>
public abstract record DuckDbPreFlightError : IExtensionPreFlightError
{
  private DuckDbPreFlightError() { }

  /// <inheritdoc/>
  public abstract string Message { get; }

  /// <inheritdoc/>
  public string Category => "duckdb";

  /// <inheritdoc/>
  public abstract string DiagnosticCode { get; }

  /// <summary>
  /// The transform SQL failed to bind against empty in-engine tables
  /// built from the declared input schemas — a syntax error, a column
  /// or relation name the declared contracts don't provide, an invalid
  /// function. <see cref="Detail"/> carries DuckDB's own binder/parser
  /// message (which names the offending token);
  /// <see cref="RelationBindings"/> lists every relation the SQL was
  /// given, so a "table does not exist" is diagnosable against what was
  /// actually bound.
  /// </summary>
  /// <param name="StepLabel">The transform step whose SQL failed to prepare.</param>
  /// <param name="RelationBindings">
  /// Human-readable description of each bound input relation:
  /// <c>relation 'name' (item 'label', schema RowType)</c>.
  /// </param>
  /// <param name="Detail">DuckDB's parser/binder message.</param>
  public sealed record SqlPreparationFailed(
    string StepLabel,
    IReadOnlyList<string> RelationBindings,
    string Detail
  ) : DuckDbPreFlightError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"DuckDB transform '{StepLabel}' SQL does not prepare against its declared "
      + $"input schemas [{string.Join(", ", RelationBindings)}]: {Detail}";

    /// <inheritdoc/>
    public override string DiagnosticCode => "FTDDB3001";
  }

  /// <summary>
  /// The transform SQL binds, but the result schema it would produce
  /// does not satisfy the output item's declared schema.
  /// <see cref="Mismatch"/> enumerates every missing, extra, and
  /// type-incompatible column (accumulated, not first-failure).
  /// </summary>
  /// <param name="StepLabel">The transform step whose result schema disagrees.</param>
  /// <param name="OutputItemLabel">The output item the result would be written to.</param>
  /// <param name="OutputSchemaName">The declared output record type's name.</param>
  /// <param name="Mismatch">Per-column disagreement list, with fix guidance.</param>
  public sealed record ResultSchemaMismatch(
    string StepLabel,
    string OutputItemLabel,
    string OutputSchemaName,
    string Mismatch
  ) : DuckDbPreFlightError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"DuckDB transform '{StepLabel}' SQL result does not satisfy output item "
      + $"'{OutputItemLabel}' declared schema {OutputSchemaName}: {Mismatch}";

    /// <inheritdoc/>
    public override string DiagnosticCode => "FTDDB3002";
  }

  /// <summary>
  /// An input relation's declared record schema has a property the
  /// DuckDB schema checks can't model (a nested/scalar kind, or a CLR
  /// type outside the type map), so no in-engine table can be built to
  /// validate the SQL against. Fail-loud by design: silently skipping
  /// the step would downgrade validation without telling anyone.
  /// </summary>
  /// <param name="StepLabel">The transform step whose input can't be modelled.</param>
  /// <param name="RelationName">The SQL relation name the input is bound to.</param>
  /// <param name="ItemLabel">The input catalog item's label.</param>
  /// <param name="Detail">Which property, and why.</param>
  public sealed record InputSchemaUnsupported(
    string StepLabel,
    string RelationName,
    string ItemLabel,
    string Detail
  ) : DuckDbPreFlightError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"DuckDB transform '{StepLabel}' input relation '{RelationName}' "
      + $"(item '{ItemLabel}') can't be schema-checked: {Detail}";

    /// <inheritdoc/>
    public override string DiagnosticCode => "FTDDB3003";
  }
}
