using Flowthru.Core.Data.Capabilities;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Common surface shared by <see cref="IFormatRowReader{TRow}"/> and
/// <see cref="IFormatRowWriter{TRow}"/>. Holds the metadata every format extension
/// declares regardless of read or write capability — runtime traits, row-shape
/// feature claims, and property-mapping strategy.
/// </summary>
/// <typeparam name="TRow">The row type the format handles.</typeparam>
/// <remarks>
/// <para>
/// Phase D (capability-segmented interfaces) introduced this base to support read-only
/// formats (e.g., Excel) that cannot — and should not — implement the write surface.
/// Read-only formats implement <see cref="IFormatRowReader{TRow}"/> only; write-capable
/// formats implement <see cref="IFormatSerializer{TRow}"/> which composes both segments.
/// </para>
/// <para>
/// End-user code typically does not implement this interface directly; format
/// extension authors implement one of the descendant interfaces depending on the
/// format's capability.
/// </para>
/// </remarks>
public interface IFormatBase<TRow>
  where TRow : notnull
{
  /// <summary>
  /// Structural capabilities of this format serializer.
  /// </summary>
  /// <remarks>
  /// Format traits focus on HOW data is serialized and whether it supports streaming.
  /// For composed adapters, these traits are merged with medium and container traits.
  /// Most formats should declare <c>CanStream = true</c> if they can deserialize row-by-row
  /// without buffering the entire stream (e.g., CSV, Parquet). Formats that require full
  /// parsing before yielding rows (e.g., JSON arrays) should set <c>CanStream = false</c>.
  /// </remarks>
  StorageTraits Traits { get; }

  /// <summary>
  /// Row-shape capabilities this format supports. Defaults to all-false; format
  /// implementations override the property to declare honestly which features round-trip.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Companion to <see cref="Traits"/>. Where <see cref="Traits"/> describes
  /// medium-level capabilities (read/write, streaming, transactional),
  /// <see cref="RowFeatures"/> describes which row-shape features the format honors
  /// (<see cref="Abstractions.IScalar"/> NewType wrappers,
  /// <see cref="Abstractions.INestedSchema"/> structures, etc.).
  /// </para>
  /// <para>
  /// The kit's <c>FormatSerializerConformance&lt;TRow&gt;</c> consults these flags to gate
  /// fixtures: when <see cref="FormatRowFeatures.SupportsIScalar"/> is <see langword="false"/>,
  /// the IScalar fixture for that format skips with an explanatory message rather than
  /// failing. When the flag is <see langword="true"/>, the fixture must round-trip
  /// successfully or the test fails.
  /// </para>
  /// <para>
  /// The default-interface-method returns <c>new FormatRowFeatures()</c> (all false) —
  /// a format that doesn't override is reported as supporting only the universal feature
  /// surface in the capability matrix.
  /// </para>
  /// </remarks>
  FormatRowFeatures RowFeatures => new();

  /// <summary>
  /// Configures how this serializer handles property-to-field name mapping for the schema.
  /// </summary>
  /// <returns>Property mapping configuration describing the mapping strategy.</returns>
  /// <remarks>
  /// <para>
  /// <strong>Contractual Obligation:</strong> Every format implementor MUST implement this
  /// method to explicitly declare how it handles property name mapping.
  /// </para>
  /// <para>
  /// <strong>Implementation Strategies:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>
  /// <strong>SerializedLabel:</strong> Consume <see cref="Serialization.PropertyMappingPlanner"/>
  /// to walk properties and resolve <c>[SerializedLabel]</c>-driven field names. Return
  /// <see cref="PropertyMappingConfiguration.FromSerializedLabel{T}()"/>.
  /// </item>
  /// <item>
  /// <strong>LibraryControlled:</strong> The underlying library handles mapping with no
  /// programmatic API; property names must match storage field names exactly. Return
  /// <see cref="PropertyMappingConfiguration.LibraryControlled(string)"/>.
  /// </item>
  /// </list>
  /// </remarks>
  PropertyMappingConfiguration GetPropertyMappingConfiguration();
}
