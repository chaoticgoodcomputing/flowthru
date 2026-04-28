using System.Reflection;

namespace Flowthru.Core.Serialization;

/// <summary>
/// Internal helper for converting enum values to/from their serialized string representations
/// using <see cref="Abstractions.SerializedEnumAttribute"/> metadata.
/// </summary>
/// <remarks>
/// This helper uses reflection to invoke the generic <see cref="EnumMetadataRegistry"/>
/// and <see cref="EnumMetadataCache{TEnum}"/> for non-generic enum type parameters.
/// This is useful in scenarios where the enum type is only known at runtime (e.g., Excel/Parquet serializers).
/// </remarks>
internal static class EnumSerializationHelper
{
  /// <summary>
  /// Converts a string value to an enum value using SerializedEnum metadata.
  /// </summary>
  /// <param name="enumType">The enum type to convert to. Must be an enum type.</param>
  /// <param name="value">The serialized string value.</param>
  /// <returns>The enum value as an object.</returns>
  /// <exception cref="ArgumentException">Thrown when enumType is not an enum type.</exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when the enum type lacks SerializedEnum attributes or the value is invalid.
  /// </exception>
  public static object ParseEnumFromString(Type enumType, string value)
  {
    if (!enumType.IsEnum)
    {
      throw new ArgumentException($"Type '{enumType.Name}' is not an enum type.", nameof(enumType));
    }

    // Use reflection to call EnumMetadataRegistry.Create<TEnum>()
    var getOrCreateMethod = typeof(EnumMetadataRegistry)
      .GetMethod(nameof(EnumMetadataRegistry.Create))!
      .MakeGenericMethod(enumType);

    var metadata = getOrCreateMethod.Invoke(null, null)!;

    // Call Parse(string) on the metadata cache
    var parseMethod = metadata.GetType().GetMethod("Parse")!;

    try
    {
      return parseMethod.Invoke(metadata, new object[] { value })!;
    }
    catch (TargetInvocationException ex) when (ex.InnerException != null)
    {
      // Unwrap the reflection exception to get the actual error
      throw ex.InnerException;
    }
  }

}
