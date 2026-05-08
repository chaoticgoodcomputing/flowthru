using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Flowthru.Data.Schema;

namespace Flowthru.Data.Storage.Csv.Internal;

/// <summary>
/// CsvHelper type converter for <see cref="IScalar"/> NewType wrappers.
/// Reads/writes the cell as the wrapper's backing primitive and
/// constructs/extracts the wrapper across the boundary.
/// </summary>
/// <typeparam name="TScalar">The wrapper type (e.g. <c>CustomerId</c>).</typeparam>
/// <typeparam name="TBacking">The backing primitive (e.g. <c>string</c>).</typeparam>
internal sealed class IScalarCsvConverter<TScalar, TBacking> : DefaultTypeConverter
  where TScalar : IScalar
{
  private readonly PropertyInfo _valueProperty;
  private readonly ConstructorInfo _wrappingConstructor;

  public IScalarCsvConverter(string valuePropertyName)
  {
    _valueProperty =
      typeof(TScalar).GetProperty(valuePropertyName, BindingFlags.Public | BindingFlags.Instance)
      ?? throw new InvalidOperationException(
        $"IScalar type '{typeof(TScalar).Name}' does not expose a public "
        + $"'{valuePropertyName}' property."
      );

    _wrappingConstructor =
      typeof(TScalar).GetConstructor(new[] { typeof(TBacking) })
      ?? throw new InvalidOperationException(
        $"IScalar type '{typeof(TScalar).Name}' does not expose a constructor taking a "
        + $"single '{typeof(TBacking).Name}' argument."
      );
  }

  public override object? ConvertFromString(
    string? text, IReaderRow row, MemberMapData memberMapData
  )
  {
    var backingConverter = row.Context.TypeConverterCache.GetConverter(typeof(TBacking));
    var rawValue = backingConverter.ConvertFromString(text, row, memberMapData);
    return _wrappingConstructor.Invoke(new[] { rawValue });
  }

  public override string? ConvertToString(
    object? value, IWriterRow row, MemberMapData memberMapData
  )
  {
    if (value is null)
    {
      return string.Empty;
    }
    var raw = _valueProperty.GetValue(value);
    var backingConverter = row.Context.TypeConverterCache.GetConverter(typeof(TBacking));
    return backingConverter.ConvertToString(raw, row, memberMapData);
  }
}
