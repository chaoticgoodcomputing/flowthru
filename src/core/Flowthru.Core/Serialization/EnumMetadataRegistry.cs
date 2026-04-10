using System.Collections.Concurrent;

namespace Flowthru.Core.Serialization;

/// <summary>
/// Global registry that provides cached <see cref="EnumMetadataCache{TEnum}"/> instances
/// for enum serialization across all storage formats.
/// </summary>
/// <remarks>
/// <para>
/// This static class ensures that enum metadata (built via reflection) is computed only once
/// per enum type and reused across all serializers and formats. It is thread-safe and uses
/// lazy initialization.
/// </para>
/// <para>
/// All Flowthru format serializers (JSON, CSV, Excel, Parquet) query this registry when
/// encountering enum types during serialization/deserialization.
/// </para>
/// </remarks>
internal static class EnumMetadataRegistry
{
  /// <summary>
  /// Cache of enum metadata indexed by enum type.
  /// </summary>
  private static readonly ConcurrentDictionary<Type, object> _cache = new();

  /// <summary>
  /// Gets or creates a cached <see cref="EnumMetadataCache{TEnum}"/> for the specified enum type.
  /// </summary>
  /// <typeparam name="TEnum">The enum type to get metadata for.</typeparam>
  /// <returns>A cached metadata instance for the specified enum type.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when the enum type does not have all members annotated with
  /// <see cref="Abstractions.SerializedEnumAttribute"/>.
  /// </exception>
  /// <remarks>
  /// This method is thread-safe. If multiple threads request metadata for the same enum type
  /// simultaneously, the metadata will be computed only once.
  /// </remarks>
  public static EnumMetadataCache<TEnum> Create<TEnum>()
    where TEnum : struct, Enum
  {
    Type enumType = typeof(TEnum);

    // Fast path: cache hit
    if (_cache.TryGetValue(enumType, out object? cached))
    {
      return (EnumMetadataCache<TEnum>)cached;
    }

    // Slow path: create and cache
    var metadata = new EnumMetadataCache<TEnum>();
    _cache.TryAdd(enumType, metadata);
    return metadata;
  }

  /// <summary>
  /// Determines whether the specified type is an enum type registered in the cache.
  /// </summary>
  /// <param name="type">The type to check.</param>
  /// <returns>true if the type is an enum and has cached metadata; otherwise, false.</returns>
  public static bool IsRegistered(Type type)
  {
    return type.IsEnum && _cache.ContainsKey(type);
  }

  /// <summary>
  /// Clears all cached enum metadata. Primarily used for testing.
  /// </summary>
  internal static void Clear()
  {
    _cache.Clear();
  }
}
