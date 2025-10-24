using System.Reflection;
using Flowthru.Meta.Models;

namespace Flowthru.Meta.Builders;

/// <summary>
/// Infers schema metadata from C# types using reflection.
/// </summary>
/// <remarks>
/// <para>
/// This class extracts property information from data types to generate schema
/// metadata for Flowthru.Viz. The inferred schema includes property names, types,
/// and nullability information.
/// </para>
/// <para>
/// <strong>Supported Type Inference:</strong>
/// </para>
/// <list type="bullet">
/// <item>Value types (int, double, DateTime, etc.)</item>
/// <item>Reference types (string, custom classes)</item>
/// <item>Nullable value types (int?, DateTime?)</item>
/// <item>Nullable reference types (string? - requires C# 8+ nullable context)</item>
/// </list>
/// <para>
/// <strong>Not Currently Supported:</strong>
/// </para>
/// <list type="bullet">
/// <item>Nested complex types (only top-level properties)</item>
/// <item>Collection types (List&lt;T&gt;, Array, etc.)</item>
/// <item>Dictionary types</item>
/// </list>
/// </remarks>
internal static class SchemaInference {
  /// <summary>
  /// Infers schema metadata from a C# type.
  /// </summary>
  /// <param name="type">The type to infer schema from</param>
  /// <returns>Schema metadata, or null if inference fails or type is too simple</returns>
  public static SchemaMetadata? InferSchema(Type type) {
    if (type == null) {
      return null;
    }

    // Only infer schemas for complex types (classes with properties)
    // Skip primitive types, strings, and system types
    if (type.IsPrimitive || type == typeof(string) || type.Namespace?.StartsWith("System") == true) {
      return null;
    }

    try {
      var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

      if (properties.Length == 0) {
        return null;
      }

      var fields = new List<SchemaField>();

      foreach (var property in properties) {
        // Skip indexed properties (this[int index])
        if (property.GetIndexParameters().Length > 0) {
          continue;
        }

        var field = new SchemaField {
          Name = property.Name,
          Type = GetSimpleTypeName(property.PropertyType),
          IsNullable = IsNullable(property)
        };

        fields.Add(field);
      }

      return new SchemaMetadata {
        Fields = fields
      };
    } catch {
      // If schema inference fails for any reason, return null
      // This ensures schema extraction never breaks DAG generation
      return null;
    }
  }

  /// <summary>
  /// Gets a simple, human-readable type name.
  /// </summary>
  /// <remarks>
  /// Examples:
  /// - int → "int"
  /// - System.String → "string"
  /// - System.DateTime → "DateTime"
  /// - int? → "int" (nullability tracked separately)
  /// - List&lt;string&gt; → "List"
  /// </remarks>
  private static string GetSimpleTypeName(Type type) {
    // Handle nullable value types (int?, DateTime?, etc.)
    var underlyingType = Nullable.GetUnderlyingType(type);
    if (underlyingType != null) {
      type = underlyingType;
    }

    // Map common system types to C# keywords
    if (type == typeof(int)) { return "int"; }
    if (type == typeof(long)) { return "long"; }
    if (type == typeof(short)) { return "short"; }
    if (type == typeof(byte)) { return "byte"; }
    if (type == typeof(bool)) { return "bool"; }
    if (type == typeof(float)) { return "float"; }
    if (type == typeof(double)) { return "double"; }
    if (type == typeof(decimal)) { return "decimal"; }
    if (type == typeof(string)) { return "string"; }
    if (type == typeof(char)) { return "char"; }
    if (type == typeof(object)) { return "object"; }

    // For other types, use the simple name without namespace
    var name = type.Name;

    // Remove generic arity indicator (e.g., "List`1" → "List")
    var backtickIndex = name.IndexOf('`');
    if (backtickIndex >= 0) {
      name = name.Substring(0, backtickIndex);
    }

    return name;
  }

  /// <summary>
  /// Determines if a property is nullable.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Nullability detection logic:
  /// </para>
  /// <list type="number">
  /// <item>Nullable value types (int?, DateTime?) → always nullable</item>
  /// <item>Reference types → check NullableAttribute for C# 8+ nullable context</item>
  /// <item>Non-nullable value types (int, DateTime) → never nullable</item>
  /// </list>
  /// </remarks>
  private static bool IsNullable(PropertyInfo property) {
    var propertyType = property.PropertyType;

    // Nullable value types (int?, DateTime?, etc.)
    if (Nullable.GetUnderlyingType(propertyType) != null) {
      return true;
    }

    // Non-nullable value types
    if (propertyType.IsValueType) {
      return false;
    }

    // Reference types - check for C# 8+ nullable reference type annotations
    // This uses NullableAttribute to determine nullability in nullable context
    var nullableAttribute = property.CustomAttributes
      .FirstOrDefault(attr => attr.AttributeType.Name == "NullableAttribute");

    if (nullableAttribute != null) {
      // NullableAttribute has a byte[] with flags
      // 0 = oblivious, 1 = not null, 2 = nullable
      if (nullableAttribute.ConstructorArguments.Count > 0) {
        var flagValue = nullableAttribute.ConstructorArguments[0].Value;

        // Check if it's a single byte value
        if (flagValue is byte flag) {
          return flag == 2; // 2 = nullable
        }

        // Check if it's a byte array (for generics)
        if (flagValue is System.Collections.ObjectModel.ReadOnlyCollection<CustomAttributeTypedArgument> array
            && array.Count > 0
            && array[0].Value is byte firstFlag) {
          return firstFlag == 2;
        }
      }
    }

    // Default: reference types are nullable unless proven otherwise
    // This is the safe default for pre-C#8 code or code without nullable context
    return true;
  }
}
