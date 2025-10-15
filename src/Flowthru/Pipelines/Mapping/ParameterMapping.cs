using System.Reflection;

namespace Flowthru.Pipelines.Mapping;

/// <summary>
/// Mapping entry that connects a property to a constant parameter value.
/// Used for input-only scenarios (parameters, configuration values).
/// </summary>
/// <remarks>
/// Parameter mappings are unidirectional - they only make sense in the input direction.
/// Attempting to use a CatalogMap with parameter mappings in the output position will
/// cause a runtime error.
/// </remarks>
internal class ParameterMapping : CatalogMapping
{
  /// <summary>
  /// The constant value to map to this property.
  /// </summary>
  public object Value { get; }

  public ParameterMapping(PropertyInfo property, object value)
      : base(property)
  {
    Value = value; // Null is allowed as a parameter value
  }

  /// <inheritdoc/>
  public override string Description =>
      $"Property '{Property.Name}' mapped to parameter value " +
      $"of type {Value?.GetType().Name ?? "null"}";
}
