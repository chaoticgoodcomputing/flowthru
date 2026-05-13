using System.Reflection;
using Apache.Arrow;
using Apache.Arrow.Types;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Single source of truth for one leaf CLR type's Arrow marshalling
/// behavior. Recursive shapes (Nullable&lt;T&gt;, IEnumerable&lt;T&gt;,
/// T[], enum) are NOT rules — they're decoded/encoded by the dispatcher
/// in <see cref="ArrowMarshaller"/> / <see cref="ArrowSchemaMapper"/>,
/// which recurse into the registry for the element type.
/// </summary>
internal interface IArrowMarshallingRule
{
  Type ClrType { get; }

  /// <summary>
  /// User-facing name for this CLR type, used in diagnostics
  /// (e.g. FT2008 unmarshallable-property) and shared with the analyzer.
  /// </summary>
  string CanonicalTypeName { get; }

  /// <summary>pandas dtype string, e.g. "int32", "datetime64[ns]".</summary>
  string PandasDtype { get; }

  /// <summary>
  /// Build the Arrow type for this rule. <paramref name="property"/> is
  /// passed by the schema mapper when iterating per-property so rules
  /// with attribute-driven parameters (e.g. a future Decimal128 rule
  /// reading <c>[ArrowDecimal]</c>) can read them. Every current rule
  /// ignores it.
  /// </summary>
  IArrowType CreateArrowType(PropertyInfo? property);

  /// <summary>
  /// Build the Arrow array for a sequence of values. <paramref name="arrowType"/>
  /// is passed so rules with type parameters (TimestampType with timezone,
  /// future Decimal128 with precision/scale) can read them. Most rules ignore it.
  /// </summary>
  IArrowArray Encode(IArrowType arrowType, List<object?> values);

  /// <summary>
  /// Returns true when the supplied Arrow array is one this rule can
  /// decode. Used by the dispatcher to confirm the array's runtime type
  /// before delegating to <see cref="Decode"/>; when this returns false
  /// the dispatcher continues to numeric-widening fallbacks.
  /// </summary>
  bool Matches(IArrowArray array);

  /// <summary>
  /// Decode one cell from an Arrow array. The array's own type carries
  /// everything Arrow→CLR decoding needs, so no property is required.
  /// Caller is responsible for confirming <see cref="Matches"/> first.
  /// </summary>
  object? Decode(IArrowArray array, int index);
}
