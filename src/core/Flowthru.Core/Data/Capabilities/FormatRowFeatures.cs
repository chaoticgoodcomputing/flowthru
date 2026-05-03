namespace Flowthru.Core.Data.Capabilities;

/// <summary>
/// Declares which row-shape features an <see cref="Storage.IFormatSerializer{TRow}"/>
/// implementation supports. Companion to <see cref="StorageTraits"/> — where
/// <see cref="StorageTraits"/> describes <em>medium-level</em> capabilities (read/write,
/// streaming, transactional), <see cref="FormatRowFeatures"/> describes
/// <em>row-shape</em> capabilities (which kinds of properties the format can round-trip).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Defaults are conservative.</strong> Every flag is <see langword="false"/>
/// unless an implementation explicitly opts in. The
/// <see cref="Storage.IFormatSerializer{TRow}.RowFeatures"/> default-interface-method
/// returns this all-false instance — a format that doesn't override the property is
/// treated as supporting only the universal feature surface (CLR primitives, enums,
/// <see cref="Abstractions.SerializedLabelAttribute"/>, <see cref="Abstractions.SerializedEnumAttribute"/>,
/// <c>Nullable&lt;T&gt;</c>, <c>required</c> members, BCL scalar structs).
/// </para>
/// <para>
/// <strong>The kit consults these flags to gate fixtures.</strong>
/// <c>FormatSerializerConformance&lt;TRow&gt;</c> reads the format's declared features and
/// skips fixtures whose required feature isn't claimed — making the conformance suite
/// honest about partial coverage. Fixtures whose feature <em>is</em> claimed but fails
/// at round-trip time produce a hard failure.
/// </para>
/// <para>
/// <strong>Adding a new feature flag is one Core change.</strong> When Flowthru gains
/// support for a new row-shape concern (a future <c>[SerializedDate]</c>, native
/// <c>System.Decimal</c> precision controls, etc.), add a flag here, add a kit fixture,
/// and every format that consumes <see cref="Serialization.PropertyMappingPlanner"/>
/// inherits the gating mechanism. Format authors update their declarations to reflect
/// genuine support.
/// </para>
/// </remarks>
public sealed record FormatRowFeatures
{
  /// <summary>
  /// Whether the format round-trips <see cref="Abstractions.IScalar"/> NewType wrappers
  /// (e.g., <c>record struct CustomerId(string Value) : IScalar</c>) — reading and
  /// writing the cell as the backing primitive type and constructing/extracting the
  /// wrapper across the boundary.
  /// </summary>
  /// <remarks>
  /// CSV, Excel, and JSON inherit this from
  /// <see cref="Serialization.PropertyMappingPlanner"/>'s <c>PropertyKind.IScalar</c>
  /// branch (Phase B migrations). Parquet declares
  /// <see cref="Serialization.OptOutOfPropertyPlannerAttribute"/> and currently does
  /// <em>not</em> handle IScalar — its DTO synthesis drops the wrapper columns silently.
  /// </remarks>
  public bool SupportsIScalar { get; init; }

  /// <summary>
  /// Whether the format round-trips <see cref="Abstractions.INestedSchema"/> shapes —
  /// schemas with object-typed sub-properties or collection columns. Flat-only formats
  /// (CSV, Excel) leave this <see langword="false"/>; their generic constraints on
  /// <see cref="Abstractions.IFlatSchema"/> already prevent the bad call site from
  /// compiling, but the declaration provides an honest signal for the capability
  /// matrix.
  /// </summary>
  public bool SupportsNested { get; init; }

  // Note: there is intentionally no flag for primitive-shape concerns like
  // `SupportsByteArrays`, `SupportsDateTimeOffset`, or `SupportsGuid`. Those are
  // intrinsic to each format's underlying serialization library (CsvHelper handles
  // byte[] as base64; Parquet has native binary columns; etc.) and either work or
  // surface format-specific errors at runtime. The matrix tracks *row-shape* concerns
  // — features where format-by-format support genuinely differs and where compile-time
  // type safety (IScalar) or structural admissibility (Nested) is the catalog
  // developer's deliberate choice.
}
