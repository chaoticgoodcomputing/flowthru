using System.ComponentModel;
using Flowthru.Data.Schema.Mapping;

namespace Flowthru.Data.Storage.Sheets.Internal;

/// <summary>
/// Coerces a loosely-typed <see cref="FieldValue"/> read back from a table to a
/// schema property's declared CLR type, driven entirely by the property's
/// <see cref="PropertyBinding"/>. <strong>The schema is the source of truth</strong>:
/// a serial <see cref="FieldKind.Number"/> becomes a <see cref="DateTime"/> when
/// the property is temporal, an <c>int</c> / <c>decimal</c> when it is numeric;
/// the field's runtime kind never overrides the property's declared type.
/// </summary>
/// <remarks>
/// The coercion cascade mirrors the Excel reader's
/// <c>ConvertCellValue</c> / <c>ConvertPrimitiveCell</c> path
/// (<see cref="Convert.ChangeType(object, Type)"/> → <see cref="TypeConverter"/>
/// → IScalar wrapping ctor → enum decode), so the two stores agree on how a
/// declared CLR type interprets a primitive value.
/// </remarks>
internal static class FieldValueDecoder
{
  /// <summary>
  /// Decode <paramref name="field"/> into the CLR value the property described
  /// by <paramref name="binding"/> expects, or <see langword="null"/> when the
  /// field is absent/empty (the caller leaves the property at its default).
  /// </summary>
  public static object? Decode(FieldValue field, PropertyBinding binding)
  {
    // Empty / null is "no value": nullable properties stay null, non-nullable
    // ones keep their CLR default. Either way the caller skips the assignment.
    if (field.Kind == FieldKind.Empty)
    {
      return null;
    }

    // Temporal-by-schema: a serial Number coerces to a CLR DateTime/DateOnly/
    // TimeOnly because the property says so, not because the field claims to be
    // temporal (read-side fields never do — a serial date arrives as a Number).
    if (field.Kind == FieldKind.Number && IsTemporalTarget(binding.EffectiveType))
    {
      var dateTime = SheetsTranslator.FromSerial(field.NumberValue);
      return CoerceTemporal(dateTime, binding.EffectiveType);
    }

    var raw = Unwrap(field);
    if (raw is null)
    {
      return null;
    }

    return Convert(raw, binding);
  }

  // Lower a field to its native CLR payload (double / bool / string), losing the
  // FieldKind tag — from here the binding's declared type drives interpretation.
  private static object? Unwrap(FieldValue field) => field.Kind switch
  {
    FieldKind.Number => field.NumberValue,
    FieldKind.Bool => field.BoolValue,
    FieldKind.Text => field.TextValue,
    // Temporal is a write-side kind; if one ever arrives on read, honour it.
    FieldKind.Temporal => field.TemporalValue,
    _ => null,
  };

  private static object Convert(object raw, PropertyBinding binding) => binding.Kind switch
  {
    PropertyKind.Enum => DecodeEnum(raw, binding),
    PropertyKind.IScalar => binding.IScalar!.WrappingConstructor.Invoke(
      new[] { System.Convert.ChangeType(raw, binding.IScalar.BackingType) }
    ),
    // Primitive covers CLR primitives, byte[], and BCL scalar structs.
    _ => ConvertPrimitive(raw, binding.EffectiveType),
  };

  private static object ConvertPrimitive(object raw, Type targetType)
  {
    if (targetType.IsInstanceOfType(raw))
    {
      return raw;
    }

    // TypeConverter first — superset of Convert.ChangeType (covers Guid,
    // TimeSpan, DateTimeOffset, etc. from their canonical string form).
    var converter = TypeDescriptor.GetConverter(targetType);
    if (converter.CanConvertFrom(raw.GetType()))
    {
      return converter.ConvertFrom(raw)!;
    }

    // IConvertible path: a Number (double) narrowing to int/long/decimal, a
    // string parsing to a primitive, etc.
    return System.Convert.ChangeType(raw, targetType);
  }

  private static object DecodeEnum(object raw, PropertyBinding binding)
  {
    var serialized = raw.ToString()
      ?? throw new SchemaMismatchException(
        $"Null value for enum field '{binding.FieldName}' "
        + $"(type '{binding.EffectiveType.Name}').");

    if (binding.Enum!.Reverse.TryGetValue(serialized, out var enumValue))
    {
      return enumValue;
    }

    throw new SchemaMismatchException(
      $"'{serialized}' is not a valid serialized value for enum "
      + $"'{binding.EffectiveType.Name}' (field '{binding.FieldName}'). "
      + $"Valid values: {string.Join(", ", binding.Enum.Reverse.Keys.Select(k => $"'{k}'"))}.");
  }

  private static bool IsTemporalTarget(Type type) =>
    type == typeof(DateTime)
    || type == typeof(DateOnly)
    || type == typeof(TimeOnly);

  private static object CoerceTemporal(DateTime dateTime, Type targetType)
  {
    if (targetType == typeof(DateOnly))
    {
      return DateOnly.FromDateTime(dateTime);
    }
    if (targetType == typeof(TimeOnly))
    {
      return TimeOnly.FromDateTime(dateTime);
    }
    return dateTime;
  }
}
