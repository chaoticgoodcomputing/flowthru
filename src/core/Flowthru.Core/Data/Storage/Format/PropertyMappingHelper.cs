using System.Reflection;
using Flowthru.Abstractions;

namespace Flowthru.Data.Storage.Format;

/// <summary>
/// Helper for mapping external field names to C# property names using SerializedLabel attribute.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Centralized property mapping logic used by all format serializers.
/// </para>
/// <para>
/// <strong>Mapping Strategy:</strong>
/// </para>
/// <list type="number">
/// <item>Check for [SerializedLabel] attribute - if present, use that label</item>
/// <item>Fall back to property name if no attribute</item>
/// <item>Use case-insensitive comparison for lookups</item>
/// </list>
/// <para>
/// <strong>Extensibility:</strong> New format serializers should use this helper to ensure
/// consistent behavior across all storage mechanisms.
/// </para>
/// </remarks>
public static class PropertyMappingHelper
{
  /// <summary>
  /// Builds a mapping from external field names to PropertyInfo objects.
  /// </summary>
  /// <typeparam name="T">The schema type</typeparam>
  /// <returns>Dictionary mapping external field names (case-insensitive) to PropertyInfo</returns>
  /// <remarks>
  /// <para>
  /// The returned dictionary uses case-insensitive string comparison, allowing flexible
  /// matching of external field names regardless of casing.
  /// </para>
  /// <para>
  /// <strong>Lookup Priority:</strong>
  /// </para>
  /// <list type="number">
  /// <item>[SerializedLabel("field_name")] - explicit attribute takes precedence</item>
  /// <item>Property.Name - fallback if no attribute present</item>
  /// </list>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Build property map for a schema
  /// var propertyMap = PropertyMappingHelper.BuildPropertyMap&lt;ShuttleSchema&gt;();
  ///
  /// // Look up property by external field name
  /// if (propertyMap.TryGetValue("shuttle_location", out var property))
  /// {
  ///     // Maps to ShuttleLocation property
  ///     var value = reader.GetValue("shuttle_location");
  ///     property.SetValue(instance, value);
  /// }
  /// </code>
  /// </example>
  public static Dictionary<string, PropertyInfo> BuildPropertyMap<T>()
  {
    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

    foreach (var property in properties)
    {
      // Check for SerializedLabel attribute
      var labelAttribute = property.GetCustomAttribute<SerializedLabelAttribute>();

      if (labelAttribute != null)
      {
        // Use explicit label from attribute
        map[labelAttribute.Label] = property;
      }
      else
      {
        // Fall back to property name
        map[property.Name] = property;
      }
    }

    return map;
  }

  /// <summary>
  /// Gets the external field name for a property.
  /// </summary>
  /// <param name="property">The property to get the field name for</param>
  /// <returns>The external field name (from SerializedLabel or property name)</returns>
  /// <remarks>
  /// <para>
  /// This method is useful for serialization scenarios where you need to write
  /// property values to external storage with the correct field names.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var properties = typeof(ShuttleSchema).GetProperties();
  /// foreach (var property in properties)
  /// {
  ///     var fieldName = PropertyMappingHelper.GetFieldName(property);
  ///     writer.WriteField(fieldName, property.GetValue(instance));
  /// }
  /// </code>
  /// </example>
  public static string GetFieldName(PropertyInfo property)
  {
    if (property == null)
    {
      throw new ArgumentNullException(nameof(property));
    }

    var labelAttribute = property.GetCustomAttribute<SerializedLabelAttribute>();
    return labelAttribute?.Label ?? property.Name;
  }
}
