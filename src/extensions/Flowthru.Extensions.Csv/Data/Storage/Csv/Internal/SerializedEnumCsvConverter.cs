using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;

namespace Flowthru.Data.Storage.Csv.Internal;

/// <summary>
/// CsvHelper type converter that maps enum members to/from their
/// <see cref="SerializedEnumAttribute"/>-declared string values.
/// Consumes the planner-emitted forward / reverse mappings off
/// <see cref="EnumBindingInfo"/> rather than reflecting independently,
/// so all format extensions agree on the same enum-string contract.
/// Honors the column's null-sentinel configuration for nullable enum
/// members.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
internal sealed class SerializedEnumCsvConverter<TEnum> : DefaultTypeConverter
  where TEnum : struct, Enum
{
  private readonly IReadOnlyDictionary<object, string> _forward;
  private readonly IReadOnlyDictionary<string, object> _reverse;

  public SerializedEnumCsvConverter(EnumBindingInfo binding)
  {
    if (binding is null) throw new ArgumentNullException(nameof(binding));
    _forward = binding.Forward;
    _reverse = binding.Reverse;
  }

  public override object? ConvertFromString(
    string? text, IReaderRow row, MemberMapData memberMapData
  )
  {
    // CsvHelper auto-applies NullValues for built-in nullable converters
    // but custom converters must consult MemberMapData themselves. When
    // the member is nullable and the cell matches a configured sentinel,
    // return null; for non-nullable enum members, returning null causes
    // CsvHelper to surface a clear "cannot convert null" error rather
    // than silently substituting default(TEnum).
    var nullValues = memberMapData.TypeConverterOptions?.NullValues;
    if (text is not null && nullValues is not null && nullValues.Contains(text))
    {
      return null;
    }

    if (string.IsNullOrEmpty(text))
    {
      return null;
    }

    if (_reverse.TryGetValue(text, out var enumValue))
    {
      return enumValue;
    }

    throw new TypeConverterException(
      this, memberMapData, text, row.Context,
      $"'{text}' is not a valid serialized value for enum '{typeof(TEnum).Name}'. "
        + $"Valid values: {string.Join(", ", _reverse.Keys.Select(k => $"'{k}'"))}."
    );
  }

  public override string? ConvertToString(
    object? value, IWriterRow row, MemberMapData memberMapData
  )
  {
    if (value is null)
    {
      return string.Empty;
    }

    if (_forward.TryGetValue(value, out var serialized))
    {
      return serialized;
    }

    throw new TypeConverterException(
      this, memberMapData, value, row.Context,
      $"Enum value '{value}' of type '{typeof(TEnum).Name}' is not defined or lacks "
        + "a [SerializedEnum] attribute."
    );
  }
}
