using System.Reflection;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Describes how a format serializer handles property-to-field name mapping.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Makes property mapping strategy explicit and discoverable for each serializer.
/// </para>
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
  /// Checks if the serializer supports SerializedLabel attributes.
  /// </summary>
  public bool SupportsSerializedLabel => Strategy == PropertyMappingStrategy.SerializedLabel;
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
  /// Underlying library controls mapping with no programmatic access.
  /// </summary>
  LibraryControlled,
}
