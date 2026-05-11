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
    RequiredFieldNames = bindings
      .Where(b => !b.IsNullable)
      .Select(b => b.FieldName)
      .ToList();
  }

  /// <summary>Try to look up a binding by external field name (case-insensitive).</summary>
  public bool TryGetByFieldName(string fieldName, [NotNullWhen(true)] out PropertyBinding? binding) =>
    ByFieldName.TryGetValue(fieldName, out binding);

  /// <summary>
  /// External field names of every required (non-nullable) property in
  /// <typeparamref name="TRow"/>, in declaration order. Format adapters
  /// consult this list during shallow inspection to verify that the
  /// data source provides every field the schema requires — extra fields
  /// in the data are tolerated (silently ignored on load), missing
  /// required fields surface as <c>ValidationErrorType.SchemaMismatch</c>.
  /// </summary>
  /// <remarks>
  /// "Required" here is approximated as <c>!IsNullable</c> on the binding.
  /// C#'s <c>required</c> keyword typically implies a non-nullable type,
  /// so this captures both signals without requiring the consumer to
  /// reflect on <c>RequiredMemberAttribute</c> separately. Nullable
  /// properties — including those with explicit <c>?</c> annotation —
  /// are treated as optional and may be absent from the data source.
  /// </remarks>
  public IReadOnlyList<string> RequiredFieldNames { get; }
}
