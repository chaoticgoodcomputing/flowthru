using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Core.Data.Serialization;

/// <summary>
/// Cached, format-agnostic snapshot of how the public properties of <typeparamref name="TRow"/>
/// map to and from external representations. Built once per
/// <see cref="PropertyMappingPlanner.Build{TRow}"/> call; consumed by every format extension
/// to wire its serializer without reimplementing the universal property-classification
/// cascade.
/// </summary>
/// <typeparam name="TRow">The schema row type the plan describes.</typeparam>
/// <remarks>
/// <para>
/// New schema-shape features (e.g., a hypothetical <c>[SerializedDate("yyyyMMdd")]</c>
/// attribute) are added to Flowthru core by extending the planner and adding a new
/// <see cref="PropertyKind"/> case. Every format consuming the planner inherits the new
/// case for free; formats that opt out via <see cref="OptOutOfPropertyPlannerAttribute"/>
/// take responsibility for their own classification.
/// </para>
/// </remarks>
public sealed class PropertyMappingPlan<TRow>
{
  /// <summary>
  /// One binding per public instance property of <typeparamref name="TRow"/>, in
  /// declaration order.
  /// </summary>
  public IReadOnlyList<PropertyBinding> Bindings { get; }

  /// <summary>
  /// Case-insensitive lookup of bindings by <see cref="PropertyBinding.FieldName"/>.
  /// Format extensions consult this to map external column / field names to the
  /// schema's properties during deserialization.
  /// </summary>
  public IReadOnlyDictionary<string, PropertyBinding> ByFieldName { get; }

  internal PropertyMappingPlan(
    IReadOnlyList<PropertyBinding> bindings,
    IReadOnlyDictionary<string, PropertyBinding> byFieldName
  )
  {
    Bindings = bindings;
    ByFieldName = byFieldName;
  }

  /// <summary>
  /// Attempts to look up a binding by its external field name (case-insensitive).
  /// </summary>
  /// <param name="fieldName">External field name to match.</param>
  /// <param name="binding">The matching binding on success; otherwise <see langword="null"/>.</param>
  /// <returns><see langword="true"/> if a binding matched; otherwise <see langword="false"/>.</returns>
  public bool TryGetByFieldName(string fieldName, [NotNullWhen(true)] out PropertyBinding? binding)
  {
    return ByFieldName.TryGetValue(fieldName, out binding);
  }
}
