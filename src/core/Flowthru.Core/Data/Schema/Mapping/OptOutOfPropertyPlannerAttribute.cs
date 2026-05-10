using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Data.Schema.Mapping;

/// <summary>
/// Marks a format serializer as deliberately not consuming
/// <see cref="PropertyMappingPlanner"/>. The presence of this attribute
/// is what the architecture-test for planner consumption accepts in lieu
/// of a direct planner reference.
/// </summary>
/// <remarks>
/// <para>
/// Canonical use: a format whose runtime DTO synthesis is structurally
/// different from the planner's reflection walk (e.g., Parquet's emit-
/// driven approach). The attribute argument is required and load-bearing:
/// it surfaces in the capability-matrix output and PR review uses it to
/// distinguish intrinsic limitations (justified) from "haven't gotten
/// around to it" notes (which become tracked planner-expansion work).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage] // Decorator only; presence is inspected by the architecture-test, not at runtime.
public sealed class OptOutOfPropertyPlannerAttribute : Attribute
{
  /// <summary>Human-readable reason this format does not consume the planner.</summary>
  public string Reason { get; }

  public OptOutOfPropertyPlannerAttribute(string reason)
  {
    if (string.IsNullOrWhiteSpace(reason))
    {
      throw new ArgumentException(
        "[OptOutOfPropertyPlanner] requires a non-empty reason. The capability matrix "
          + "and PR-review process both depend on this string.",
        nameof(reason)
      );
    }
    Reason = reason;
  }
}
