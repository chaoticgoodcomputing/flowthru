namespace Flowthru.Extensions.EFCore.Lifecycle;

/// <summary>
/// Configuration knobs for
/// <see cref="EFCoreResources.EphemeralSchema{TContext}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Mutable to support the fluent <c>configure: o =&gt; { o.Property = ... }</c>
/// pattern. Defaults reflect the strict-cleanup expectation: any leftover
/// staging artifact is wrong unless the consumer explicitly opts in to
/// preservation for debugging.
/// </para>
/// </remarks>
public sealed class EphemeralSchemaOptions
{
  /// <summary>
  /// When <c>true</c>, the schema is preserved if the flow fails, so the
  /// developer can inspect intermediate state. Default: <c>false</c> — the
  /// schema is always dropped on flow exit.
  /// </summary>
  /// <remarks>
  /// The framework provides the body's primary exception to the resource's
  /// release closure; this option simply gates whether the drop runs when
  /// that exception is non-null.
  /// </remarks>
  public bool PreserveOnFailure { get; set; } = false;

  /// <summary>
  /// Optional transformation applied to the DDL produced from the
  /// <typeparamref name="TContext">DbContext</typeparamref>'s model before it
  /// runs against the database. Use this to strip statements that conflict
  /// with shared catalog state — for example, <c>CREATE TYPE</c> for enums
  /// already defined elsewhere in the database.
  /// </summary>
  /// <remarks>
  /// The DDL is generated via <c>RelationalDatabaseCreator.GenerateCreateScript()</c>
  /// and reflects the current schema as defined by the model's
  /// <c>HasDefaultSchema</c> configuration plus any explicit <c>ToTable(..., schema)</c>
  /// declarations.
  /// </remarks>
  public Func<string, string>? DdlFilter { get; set; }
}
