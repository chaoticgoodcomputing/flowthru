using System.Reflection;

namespace Flowthru.Data.Storage;

/// <summary>
/// Describes how a format serializer handles property-to-field name mapping.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Makes property mapping strategy explicit and discoverable for each serializer.
/// </para>
/// <para>
/// <strong>Strategy Types:</strong>
/// </para>
/// <list type="bullet">
/// <item><see cref="SerializedLabel"/> - Respects [SerializedLabel] attributes</item>
/// <item><see cref="NativeAttributes"/> - Uses format-specific attributes (e.g., [LoadColumn])</item>
/// <item><see cref="LibraryControlled"/> - Underlying library controls mapping</item>
/// <item><see cref="Adapter"/> - Bridges SerializedLabel with native attributes</item>
/// </list>
/// </remarks>
public sealed class PropertyMappingConfiguration
{
  /// <summary>
  /// The strategy used for property mapping.
  /// </summary>
  public PropertyMappingStrategy Strategy { get; }

  /// <summary>
  /// Optional description of the mapping behavior.
  /// </summary>
  public string? Description { get; }

  /// <summary>
  /// For NativeAttributes strategy: the name of the attribute type(s) used.
  /// For Adapter strategy: the adapter type.
  /// </summary>
  public Type? MetadataType { get; }

  private PropertyMappingConfiguration(
    PropertyMappingStrategy strategy,
    string? description = null,
    Type? metadataType = null
  )
  {
    Strategy = strategy;
    Description = description;
    MetadataType = metadataType;
  }

  /// <summary>
  /// Serializer uses SerializedLabel attributes via PropertyMappingHelper.
  /// </summary>
  /// <typeparam name="TRow">The schema type</typeparam>
  /// <returns>Configuration for SerializedLabel-based mapping</returns>
  public static PropertyMappingConfiguration FromSerializedLabel<TRow>()
  {
    return new PropertyMappingConfiguration(
      PropertyMappingStrategy.SerializedLabel,
      description: "Uses [SerializedLabel] attributes for property-to-field mapping. "
        + "Falls back to property name if no attribute present.",
      metadataType: typeof(TRow)
    );
  }

  /// <summary>
  /// Serializer uses format-specific attributes (e.g., ML.NET's [LoadColumn], [ColumnName]).
  /// </summary>
  /// <param name="attributeDescription">Description of native attributes used</param>
  /// <returns>Configuration for native attribute mapping</returns>
  public static PropertyMappingConfiguration FromNativeAttributes(string attributeDescription)
  {
    return new PropertyMappingConfiguration(
      PropertyMappingStrategy.NativeAttributes,
      description: $"Uses format-specific attributes: {attributeDescription}"
    );
  }

  /// <summary>
  /// Underlying library controls mapping with no programmatic API.
  /// Property names must match storage field names exactly.
  /// </summary>
  /// <param name="limitation">Optional description of the limitation</param>
  /// <returns>Configuration for library-controlled mapping</returns>
  public static PropertyMappingConfiguration LibraryControlled(string? limitation = null)
  {
    return new PropertyMappingConfiguration(
      PropertyMappingStrategy.LibraryControlled,
      description: limitation
        ?? "Library-controlled mapping. Property names must match storage field names exactly."
    );
  }

  /// <summary>
  /// Serializer uses an adapter to bridge SerializedLabel with native attributes.
  /// </summary>
  /// <typeparam name="TAdapter">The adapter type that performs the bridging</typeparam>
  /// <returns>Configuration for adapter-based mapping</returns>
  public static PropertyMappingConfiguration FromAdapter<TAdapter>()
  {
    return new PropertyMappingConfiguration(
      PropertyMappingStrategy.Adapter,
      description: $"Uses adapter {typeof(TAdapter).Name} to bridge [SerializedLabel] with native format attributes.",
      metadataType: typeof(TAdapter)
    );
  }

  /// <summary>
  /// Checks if the serializer supports SerializedLabel attributes (directly or via adapter).
  /// </summary>
  public bool SupportsSerializedLabel =>
    Strategy == PropertyMappingStrategy.SerializedLabel
    || Strategy == PropertyMappingStrategy.Adapter;

  /// <summary>
  /// Gets a human-readable description of the mapping strategy.
  /// </summary>
  public override string ToString()
  {
    return Description ?? Strategy.ToString();
  }
}

/// <summary>
/// Property mapping strategy used by a format serializer.
/// </summary>
public enum PropertyMappingStrategy
{
  /// <summary>
  /// Serializer respects [SerializedLabel] attributes using PropertyMappingHelper.
  /// </summary>
  SerializedLabel,

  /// <summary>
  /// Serializer uses format-specific attributes (e.g., ML.NET [LoadColumn], CsvHelper [Name]).
  /// </summary>
  NativeAttributes,

  /// <summary>
  /// Underlying library controls mapping with no programmatic access.
  /// </summary>
  LibraryControlled,

  /// <summary>
  /// Serializer uses an adapter to translate SerializedLabel to native attributes.
  /// </summary>
  Adapter,
}
