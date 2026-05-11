namespace Flowthru.Data.Catalog;

/// <summary>
/// Universal capability metadata for any DAG node. The canonical
/// archetype-specific extension is <c>StorageTraits</c> for catalog items;
/// step-level traits (<c>IsIdempotent</c>, <c>HasSideEffects</c>) live in
/// per-step metadata records emitted by the source generator rather than
/// inheriting from this type.
/// </summary>
public record NodeTraits
{
  /// <summary>Whether this node requires network access.</summary>
  public bool RequiresNetwork { get; init; } = false;

  /// <summary>Whether this node supports pre-flight inspection.</summary>
  public bool CanInspect { get; init; } = true;
}
