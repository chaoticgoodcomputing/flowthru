using System.Reflection;

namespace Flowthru.Meta.Extensions;

/// <summary>
/// Extension methods that replicate pandas DataFrame operations for IEnumerable collections.
/// </summary>
public static class PandasExtensions
{
  /// <summary>
  /// Replicates pandas DataFrame.dropna() behavior by removing records with any null values.
  /// Checks all properties of type T for null values:
  /// - Nullable value types (int?, decimal?, etc.) - checks HasValue
  /// - Nullable reference types (string?, object?, etc.) - checks for null or empty strings
  /// - Non-nullable types are always considered valid
  /// </summary>
  /// <typeparam name="T">The record/class type</typeparam>
  /// <param name="source">Source enumerable</param>
  /// <returns>Filtered enumerable with no null values in any property</returns>
  public static IEnumerable<T> DropNa<T>(this IEnumerable<T> source) where T : class
  {
    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    return source.Where(item =>
    {
      if (item == null) return false;

      foreach (var prop in properties)
      {
        var value = prop.GetValue(item);

        // Check if property is nullable value type (int?, decimal?, bool?, etc.)
        var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
        if (underlyingType != null)
        {
          // Nullable value type - null means drop
          if (value == null) return false;
        }
        // Check if property is string (special case - check for null or empty)
        else if (prop.PropertyType == typeof(string))
        {
          if (string.IsNullOrWhiteSpace(value as string)) return false;
        }
        // Check if property is nullable reference type
        else if (!prop.PropertyType.IsValueType)
        {
          // Nullable reference type - null means drop
          if (value == null) return false;
        }
        // Non-nullable value types (int, bool, decimal, etc.) are always valid
      }

      return true;
    });
  }

  /// <summary>
  /// Replicates pandas DataFrame.dropna(subset=[...]) behavior by checking only specific properties.
  /// </summary>
  /// <typeparam name="T">The record/class type</typeparam>
  /// <param name="source">Source enumerable</param>
  /// <param name="propertyNames">Names of properties to check for null values</param>
  /// <returns>Filtered enumerable where specified properties have no null values</returns>
  public static IEnumerable<T> DropNa<T>(this IEnumerable<T> source, params string[] propertyNames) where T : class
  {
    var propertyMap = typeof(T)
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => propertyNames.Contains(p.Name))
      .ToDictionary(p => p.Name);

    if (propertyMap.Count != propertyNames.Length)
    {
      var missing = propertyNames.Except(propertyMap.Keys).ToList();
      throw new ArgumentException(
        $"Properties not found on type {typeof(T).Name}: {string.Join(", ", missing)}");
    }

    return source.Where(item =>
    {
      if (item == null) return false;

      foreach (var propName in propertyNames)
      {
        var prop = propertyMap[propName];
        var value = prop.GetValue(item);

        var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
        if (underlyingType != null)
        {
          if (value == null) return false;
        }
        else if (prop.PropertyType == typeof(string))
        {
          if (string.IsNullOrWhiteSpace(value as string)) return false;
        }
        else if (!prop.PropertyType.IsValueType)
        {
          if (value == null) return false;
        }
      }

      return true;
    });
  }
}
