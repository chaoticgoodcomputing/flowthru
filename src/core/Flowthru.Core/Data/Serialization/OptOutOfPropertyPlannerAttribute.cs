namespace Flowthru.Core.Data.Serialization;

/// <summary>
/// Marks an <see cref="Storage.IFormatSerializer{TRow}"/> implementation as deliberately
/// not consuming <see cref="PropertyMappingPlanner"/>. The presence of this attribute is
/// what the <c>_test:planner-consumption</c> meta-test (Phase B5) accepts in lieu of a
/// direct planner reference.
/// </summary>
/// <remarks>
/// <para>
/// The canonical intended consumer is <c>ParquetFormatSerializer</c>: Parquet's
/// runtime DTO synthesis via <c>System.Reflection.Emit</c> is structurally different
/// from the reflection walks the planner subsumes for CSV / Excel / JSON, and a
/// planner-driven Parquet migration is a follow-up effort outside Phase B's scope.
/// </para>
/// <para>
/// <strong>The reason argument is required and load-bearing.</strong> The capability
/// matrix surfaces it on the "manual mapping" line for the format, and PR review of any
/// future opt-out introduction or revision uses the reason to evaluate whether the
/// limitation is intrinsic to the format (justified) versus transient ("haven't gotten
/// around to it"; transient opt-outs become tracked planner-expansion work).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class OptOutOfPropertyPlannerAttribute : Attribute
{
  /// <summary>
  /// The human-readable reason this format does not consume the planner. Renders into
  /// the auto-generated capability matrix and into PR-review surface area.
  /// </summary>
  public string Reason { get; }

  /// <param name="reason">
  /// Description of why the planner cannot subsume this format's property walk. Should
  /// describe an intrinsic structural limitation — not a "TODO: migrate later" note.
  /// </param>
  public OptOutOfPropertyPlannerAttribute(string reason)
  {
    if (string.IsNullOrWhiteSpace(reason))
    {
      throw new ArgumentException(
        "[OptOutOfPropertyPlanner] requires a non-empty reason. The capability matrix and PR-review process both depend on this string.",
        nameof(reason)
      );
    }

    Reason = reason;
  }
}
