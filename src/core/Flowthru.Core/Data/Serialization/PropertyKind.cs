namespace Flowthru.Core.Data.Serialization;

/// <summary>
/// The structural classification of a schema property as resolved by
/// <see cref="PropertyMappingPlanner"/>. Each format extension consumes a binding's
/// <see cref="PropertyKind"/> to decide how to encode/decode the cell at the format
/// layer. Adding a new schema-shape feature to Flowthru core means adding a new value
/// here (and the corresponding planner branch) — every format consuming the planner
/// inherits the new case for free.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the Tier 1–5 cascade in
/// <c>Flowthru.Core.SourceGenerators.SchemaAnalysis.SchemaPropertyClassifier.IsFlatPropertyType</c>.
/// The compile-time classifier decides which marker interfaces a schema implements; the
/// planner classifies individual properties for runtime serialization.
/// </para>
/// <para>
/// A nullable wrapper (<c>Nullable&lt;T&gt;</c> or <c>T?</c>) does NOT change the
/// <see cref="PropertyKind"/>: the planner unwraps the nullable and reports the kind of
/// the underlying type via <see cref="PropertyBinding.EffectiveType"/>, with the
/// nullability reported separately through <see cref="PropertyBinding.IsNullable"/>.
/// </para>
/// </remarks>
public enum PropertyKind
{
  /// <summary>
  /// CLR primitives, <c>byte[]</c> opaque blobs, BCL scalar structs (<c>Guid</c>,
  /// <c>DateTime</c>, <c>TimeSpan</c>, <c>DateTimeOffset</c>, <c>DateOnly</c>,
  /// <c>TimeOnly</c>, <c>Half</c>, <c>Int128</c>, <c>UInt128</c>). Format extensions
  /// typically delegate primitive cell encoding to the underlying library
  /// (CsvHelper's default converters, System.Text.Json's primitive readers, etc.).
  /// </summary>
  Primitive,

  /// <summary>
  /// A <c>System.Enum</c> type. The format extension is expected to honor
  /// <see cref="Abstractions.SerializedEnumAttribute"/>-decorated values via
  /// <see cref="Serialization.EnumSerializationHelper"/> or its own enum metadata
  /// integration. <see cref="PropertyBinding.Enum"/> exposes the enum type for the
  /// format's converter to wire against.
  /// </summary>
  Enum,

  /// <summary>
  /// A user-defined NewType wrapping a single primitive (a type implementing
  /// <see cref="Abstractions.IScalar"/> with exactly one public readable property).
  /// The format extension is expected to read/write the cell as the backing type,
  /// then construct the wrapper via its single-arg constructor.
  /// <see cref="PropertyBinding.IScalar"/> exposes everything the converter needs:
  /// backing type, value property, and wrapping constructor.
  /// </summary>
  IScalar,

  /// <summary>
  /// A non-primitive structured type (typically a schema marked
  /// <see cref="Abstractions.INestedSchema"/>, but also any type that doesn't fit the
  /// primitive/enum/IScalar cases). Format extensions that support nested data
  /// (<c>JsonFormatSerializer</c>, <c>ParquetFormatSerializer</c>) recurse into the
  /// type via their own infrastructure. Flat-only formats (CSV, Excel) reject nested
  /// bindings — though <see cref="IFormatSerializer{TRow}"/>'s generic constraints on
  /// <see cref="Abstractions.IFlatSchema"/> already prevent the bad call site from
  /// compiling, so receiving a <see cref="Nested"/> binding in a flat-only format
  /// indicates a bug rather than a user error.
  /// </summary>
  Nested,
}
