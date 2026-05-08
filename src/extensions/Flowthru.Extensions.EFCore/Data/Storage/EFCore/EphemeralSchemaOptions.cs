namespace Flowthru.Data.Storage.EFCore;

/// <summary>
/// Configuration knobs for
/// <see cref="EFCoreLifecycleExtensions.EphemeralSchema"/>.
/// </summary>
/// <remarks>
/// Mutable to support the fluent <c>configure: o =&gt; o.Property = ...</c>
/// pattern. Defaults reflect the strict-cleanup expectation: a
/// leftover staging artifact is wrong unless the caller explicitly
/// opts in to preservation for debugging.
/// </remarks>
public sealed class EphemeralSchemaOptions
{
  /// <summary>
  /// When <c>true</c>, the schema is preserved if the flow fails so
  /// the developer can inspect intermediate state. Default
  /// <c>false</c> — the schema is always dropped on flow exit.
  /// </summary>
  public bool PreserveOnFailure { get; set; } = false;

  /// <summary>
  /// Optional transformation applied to the DDL produced from the
  /// DbContext model before it runs against the database. Use this
  /// to strip statements that conflict with shared catalog state —
  /// e.g. <c>CREATE TYPE</c> for enums already defined elsewhere.
  /// </summary>
  /// <remarks>
  /// The DDL is generated via
  /// <c>RelationalDatabaseCreator.GenerateCreateScript()</c> and
  /// reflects the model's <c>HasDefaultSchema</c> configuration plus
  /// any explicit <c>ToTable(..., schema)</c> declarations.
  /// </remarks>
  public Func<string, string>? DdlFilter { get; set; }
}
