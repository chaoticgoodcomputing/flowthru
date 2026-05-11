namespace Flowthru.Data.Schema.Mapping;

/// <summary>
/// Structural classification of a schema property under the planner's
/// Tier 1–5 cascade. Format extensions consume a binding's
/// <see cref="PropertyKind"/> to decide how to encode/decode the cell at
/// the format layer. A nullable wrapper does not change the kind — the
/// planner unwraps and reports the underlying type's kind, with
/// nullability tracked separately on the binding.
/// </summary>
public enum PropertyKind
{
  /// <summary>
  /// CLR primitives, <c>byte[]</c> opaque blobs, BCL scalar structs
  /// (<c>Guid</c>, <c>DateTime</c>, <c>TimeSpan</c>, <c>DateTimeOffset</c>,
  /// <c>DateOnly</c>, <c>TimeOnly</c>, <c>Half</c>, <c>Int128</c>,
  /// <c>UInt128</c>).
  /// </summary>
  Primitive,

  /// <summary>
  /// A <c>System.Enum</c> type. Format extensions honor
  /// <see cref="SerializedEnumAttribute"/>-decorated values.
  /// </summary>
  Enum,

  /// <summary>
  /// A user-defined NewType wrapping a single primitive — implementer of
  /// <see cref="IScalar"/> with exactly one public readable property.
  /// Format extensions read/write the cell as the backing type and
  /// construct the wrapper via its single-arg constructor.
  /// </summary>
  IScalar,

  /// <summary>
  /// A non-primitive structured type — nested objects or collections.
  /// Format extensions that support nested data (JSON, Parquet) recurse;
  /// flat-only formats (CSV, Excel) reject (their generic constraints on
  /// <see cref="IFlatSchema"/> already prevent the bad call site from
  /// compiling).
  /// </summary>
  Nested,
}
