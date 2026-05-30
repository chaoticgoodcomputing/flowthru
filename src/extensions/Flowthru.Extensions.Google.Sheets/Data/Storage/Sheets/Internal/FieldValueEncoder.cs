using System.ComponentModel;
using Flowthru.Data.Schema.Mapping;

namespace Flowthru.Data.Storage.Sheets.Internal;

/// <summary>
/// Encodes a schema property's CLR value into a loosely-typed
/// <see cref="FieldValue"/> for writing to a table — the exact inverse of
/// <see cref="FieldValueDecoder"/>, driven entirely by the property's
/// <see cref="PropertyBinding"/>. The binding's declared CLR type decides the
/// field kind: a temporal property emits <see cref="FieldKind.Temporal"/> (the
/// gateway turns that into a serial number plus a <c>numberFormat</c>), a
/// numeric property emits <see cref="FieldKind.Number"/>, and everything text-
/// shaped (string, enum's serialized form, an unwrapped IScalar, a Guid, a
/// TimeSpan, …) emits <see cref="FieldKind.Text"/>.
/// </summary>
/// <remarks>
/// The kind cascade mirrors the decoder: temporal-by-schema first, then the
/// per-<see cref="PropertyKind"/> dispatch (enum → serialized string, IScalar →
/// backing value, primitive → native number/bool or text). A <c>byte[]</c>
/// property has no faithful table representation and is rejected by the schema
/// builder before any encode happens, so it never reaches this type.
/// </remarks>
internal static class FieldValueEncoder
{
  /// <summary>
  /// Encode the value of the property described by <paramref name="binding"/>
  /// on <paramref name="row"/> into a neutral <see cref="FieldValue"/>. A null
  /// value (a null reference or a null <see cref="Nullable{T}"/>) becomes
  /// <see cref="FieldValue.Empty"/>.
  /// </summary>
  public static FieldValue Encode(object row, PropertyBinding binding)
  {
    var value = binding.Property.GetValue(row);
    if (value is null)
    {
      return FieldValue.Empty;
    }

    // Temporal-by-schema: a DateTime/DateOnly/TimeOnly property emits a Temporal
    // field so the gateway encodes the serial + numberFormat. The CLR runtime
    // type matches the declared type here (we read it off the property).
    if (binding.EffectiveType == typeof(DateTime))
    {
      return FieldValue.Temporal((DateTime)value, TemporalKind.DateTime);
    }
    if (binding.EffectiveType == typeof(DateOnly))
    {
      return FieldValue.Temporal(((DateOnly)value).ToDateTime(TimeOnly.MinValue), TemporalKind.Date);
    }
    if (binding.EffectiveType == typeof(TimeOnly))
    {
      var time = (TimeOnly)value;
      return FieldValue.Temporal(
        SheetsTranslator.SerialEpoch.Add(time.ToTimeSpan()), TemporalKind.Time);
    }

    return binding.Kind switch
    {
      PropertyKind.Enum => EncodeEnum(value, binding),
      PropertyKind.IScalar => EncodeScalar(value, binding),
      _ => EncodePrimitive(value, binding.EffectiveType),
    };
  }

  private static FieldValue EncodeEnum(object value, PropertyBinding binding)
  {
    if (binding.Enum!.Forward.TryGetValue(value, out var serialized))
    {
      return FieldValue.Text(serialized);
    }

    throw new SchemaMismatchException(
      $"Enum value '{value}' for field '{binding.FieldName}' "
      + $"(type '{binding.EffectiveType.Name}') has no serialized representation.");
  }

  private static FieldValue EncodeScalar(object value, PropertyBinding binding)
  {
    // Unwrap the NewType to its backing primitive, then encode that primitive
    // exactly as a bare property of the backing type would be.
    var backing = binding.IScalar!.ValueProperty.GetValue(value);
    return backing is null
      ? FieldValue.Empty
      : EncodePrimitive(backing, binding.IScalar.BackingType);
  }

  // A native number or bool maps to its own field kind; anything else (string,
  // Guid, TimeSpan, DateTimeOffset, Int128, …) goes to text via its canonical
  // string form, the inverse of the decoder's TypeConverter/ChangeType path.
  private static FieldValue EncodePrimitive(object value, Type type)
  {
    if (type == typeof(bool))
    {
      return FieldValue.Bool((bool)value);
    }

    if (IsNumeric(type))
    {
      return FieldValue.Number(Convert.ToDouble(value));
    }

    if (type == typeof(string))
    {
      return FieldValue.Text((string)value);
    }

    // Canonical string form for the BCL scalar structs (Guid, TimeSpan,
    // DateTimeOffset, …). Prefer the type's own TypeConverter — it is the
    // inverse of the decoder's ConvertPrimitive path — then fall back to
    // ToString for the few that lack one (Int128/UInt128/Half round-trip
    // through ToString verbatim).
    var converter = TypeDescriptor.GetConverter(type);
    if (converter.CanConvertTo(typeof(string)))
    {
      return FieldValue.Text(converter.ConvertToInvariantString(value) ?? string.Empty);
    }

    return FieldValue.Text(value.ToString() ?? string.Empty);
  }

  private static bool IsNumeric(Type type) =>
    type == typeof(int)
    || type == typeof(long)
    || type == typeof(short)
    || type == typeof(byte)
    || type == typeof(sbyte)
    || type == typeof(uint)
    || type == typeof(ulong)
    || type == typeof(ushort)
    || type == typeof(double)
    || type == typeof(float)
    || type == typeof(decimal);
}
