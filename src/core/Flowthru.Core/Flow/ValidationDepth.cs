namespace Flowthru.Flow;

/// <summary>
/// How thorough pre-flight inspection should be when a flow is run.
/// Materialises the levels exposed on <c>IItem</c>'s
/// <c>InspectShallow</c>/<c>InspectDeep</c>/<c>InspectTarget</c>.
/// </summary>
public enum ValidationDepth
{
  /// <summary>No pre-flight inspection — go straight to execution.</summary>
  None,

  /// <summary>Existence + small-sample schema check on inputs (default).</summary>
  Shallow,

  /// <summary>Full-dataset schema validation on inputs.</summary>
  Deep,
}
