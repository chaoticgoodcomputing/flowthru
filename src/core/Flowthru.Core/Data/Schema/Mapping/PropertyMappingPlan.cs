using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Data.Schema.Mapping;

/// <summary>
/// Cached, format-agnostic snapshot of how the public properties of
/// <typeparamref name="TRow"/> map to and from external representations.
/// Built once per <see cref="PropertyMappingPlanner.Build{TRow}()"/> call;
/// consumed by every format extension to wire its serializer without
/// reimplementing the universal property-classification cascade.
/// </summary>
public sealed class PropertyMappingPlan<TRow>
{
  /// <summary>One binding per public instance property of <typeparamref name="TRow"/>, in declaration order.</summary>
  public IReadOnlyList<PropertyBinding> Bindings { get; }

  /// <summary>
  /// Case-insensitive lookup of bindings by external field name. Format
  /// extensions consult this to map external column / field names to
  /// schema properties during deserialization.
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

  /// <summary>Try to look up a binding by external field name (case-insensitive).</summary>
  public bool TryGetByFieldName(string fieldName, [NotNullWhen(true)] out PropertyBinding? binding) =>
    ByFieldName.TryGetValue(fieldName, out binding);
}
