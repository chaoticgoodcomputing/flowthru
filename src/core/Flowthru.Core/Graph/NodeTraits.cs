namespace Flowthru.Core.Graph;

/// <summary>
/// Base capability metadata for all DAG node types.
/// </summary>
/// <remarks>
/// <para>
/// Describes universal properties that apply to any node in the DAG. The canonical
/// archetype-specific extension is
/// <see cref="Flowthru.Core.Data.Capabilities.StorageTraits"/> for data I/O nodes
/// (catalog entries — IItem). Step-level traits like <c>IsIdempotent</c> /
/// <c>HasSideEffects</c> are emitted by the source generator into per-step
/// <c>StepTraits</c> values rather than carried via NodeTraits inheritance.
/// </para>
/// </remarks>
public record NodeTraits
{
  /// <summary>
  /// Whether this node requires network access to operate.
  /// </summary>
  public bool RequiresNetwork { get; init; } = false;

  /// <summary>
  /// Whether this node supports pre-flight inspection / validation.
  /// </summary>
  public bool CanInspect { get; init; } = true;
}
