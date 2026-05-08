using System.ComponentModel;
using System.Text;
using ExcelDataReader;
using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;

namespace Flowthru.Data.Storage.Excel;

/// <summary>
/// Read-only format reader for Excel (.xlsx) workbooks via
/// ExcelDataReader. Reads a single named sheet; structurally read-only
/// — does not implement <see cref="IFormatRowWriter{TRow}"/> — so the
/// composed adapter's <see cref="StorageTraits.CanWrite"/> resolves to
/// <c>false</c> at the type level rather than via a runtime trait
/// check.
/// </summary>
/// <typeparam name="TRow">
/// Row schema. Excel cells are tabular, so the schema must be flat
/// (<see cref="IFlatSchema"/>) and text-serialisable
/// (<see cref="ITextSerializable"/>).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Capability claims:</strong> implements <see cref="ISupportsIScalar"/>
/// (NewType wrapping round-trips via the planner). Does not implement
/// <see cref="IFormatStreamReader{TRow}"/> — ExcelDataReader buffers
/// the workbook structure to find the target sheet, so streaming
/// claims would be misleading. <see cref="StorageTraits.CanStream"/>
/// is correspondingly <c>false</c>.
/// </para>
/// <para>
/// <strong>Schema-mismatch translation:</strong> a missing or renamed
/// sheet, missing required columns, or unparseable cell values raise
/// <see cref="SchemaMismatchException"/> so the composed adapter's
/// boundary lifts them to typed
/// <see cref="Validation.Runtime.RuntimeError.SchemaMismatch"/> on
/// the load path and <see cref="ValidationErrorType.SchemaMismatch"/>
/// on the inspect path.
/// </para>
/// <para>
/// <strong>Null handling:</strong> empty cells (<c>DBNull</c>) round-
/// trip as <c>null</c> for nullable properties. Catalog authors can
/// extend the set of null sentinels via the <c>nullValues</c>
/// constructor parameter — for example
/// <c>["", "NA", "N/A", "NULL"]</c> for messy spreadsheet exports.
/// Non-nullable properties are unaffected.
/// </para>
/// </remarks>
public sealed class ExcelFormatSerializer<TRow>
  : IFormatRowReader<TRow>, ISupportsIScalar
  where TRow : notnull, IFlatSchema, ITextSerializable
{
  private readonly string _sheetName;
  private readonly IReadOnlyList<string> _nullValues;

  /// <summary>
  /// Default null-sentinel list — only empty / DBNull cells deserialize
  /// to null on read.
  /// </summary>
  public static readonly IReadOnlyList<string> DefaultNullValues = new[] { string.Empty };

  /// <summary>Read-only adapter targeting the named sheet. Empty cells round-trip as null.</summary>
  public ExcelFormatSerializer(string sheetName)
    : this(sheetName, DefaultNullValues) { }

  /// <summary>Read-only adapter targeting the named sheet, with a custom null-sentinel list.</summary>
  public ExcelFormatSerializer(string sheetName, IReadOnlyList<string> nullValues)
  {
    _sheetName = sheetName ?? throw new ArgumentNullException(nameof(sheetName));
    _nullValues = nullValues ?? throw new ArgumentNullException(nameof(nullValues));
  }

  /// <summary>The sheet name this adapter reads from.</summary>
  public string SheetName => _sheetName;

  /// <summary>The null-sentinel list applied to nullable properties on read.</summary>
  public IReadOnlyList<string> NullValues => _nullValues;

  /// <inheritdoc/>
  public StorageTraits Traits => new() { CanWrite = false, CanStream = false };

  /// <inheritdoc/>
  public async IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
  {
    if (stream is null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    // ExcelDataReader requires a seekable stream; buffer if needed.
    if (!stream.CanSeek)
    {
      var buffered = new MemoryStream();
      await stream.CopyToAsync(buffered).ConfigureAwait(false);
      buffered.Position = 0;
      stream = buffered;
    }

    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    using var reader = ExcelReaderFactory.CreateReader(stream);

    var plan = PropertyMappingPlanner.Build<TRow>(
      new PropertyMappingPlannerOptions { NullSentinels = _nullValues }
    );

    do
    {
      if (reader.Name != _sheetName)
      {
        continue;
      }

      // Header row — bail out gracefully if the sheet is empty.
      if (!reader.Read())
      {
        yield break;
      }

      var headers = new string[reader.FieldCount];
      var columnIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
      for (int i = 0; i < reader.FieldCount; i++)
      {
        headers[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
        columnIndexMap[headers[i]] = i;
      }

      while (reader.Read())
      {
        var row = SchemaActivator.CreateInstance<TRow>();
        foreach (var binding in plan.Bindings)
        {
          if (!columnIndexMap.TryGetValue(binding.FieldName, out var columnIndex))
          {
            continue;
          }

          var value = reader.GetValue(columnIndex);

          // Null-sentinel handling: DBNull / null / matching sentinel string → leave default.
          if (binding.IsNullable)
          {
            if (value is null || value == DBNull.Value) continue;
            if (value is string strValue && binding.NullSentinels.Contains(strValue)) continue;
          }
          else if (value is null || value == DBNull.Value)
          {
            continue;
          }

          var converted = ConvertCellValue(value, binding);
          binding.Property.SetValue(row, converted);
        }
        yield return row;
      }

      yield break;
    } while (reader.NextResult());

    // Sheet not found: structural mismatch between the workbook and the
    // catalog item's declared shape. Surface as schema-mismatch so the
    // composed adapter's boundary lifts it to typed RuntimeError.SchemaMismatch
    // and ValidationErrorType.SchemaMismatch.
    throw new SchemaMismatchException(
      $"Sheet '{_sheetName}' not found in Excel workbook for schema '{typeof(TRow).Name}'."
    );
  }

  /// <summary>
  /// Per-binding cell conversion. The planner classifies each property
  /// (Primitive / Enum / IScalar) and provides per-kind metadata; we
  /// dispatch on <see cref="PropertyKind"/> here.
  /// </summary>
  private static object ConvertCellValue(object cellValue, PropertyBinding binding) =>
    binding.Kind switch
    {
      PropertyKind.Enum => DecodeEnum(cellValue, binding),
      PropertyKind.IScalar => binding.IScalar!.WrappingConstructor.Invoke(
        new[] { Convert.ChangeType(cellValue, binding.IScalar.BackingType) }
      ),
      // Primitive covers CLR primitives, byte[], and BCL scalar structs
      // (Guid, TimeSpan, DateTimeOffset, DateOnly, TimeOnly, Half,
      // Int128/UInt128). Convert.ChangeType handles the IConvertible
      // primitives but not Guid/TimeSpan/etc. — for those the type
      // descriptor's converter knows how to go from string to struct.
      PropertyKind.Primitive => ConvertPrimitiveCell(cellValue, binding.EffectiveType),
      // Nested would reach here only if an IFlatSchema-violating type
      // slipped past the generic constraint — defensive fall-through.
      PropertyKind.Nested => ConvertPrimitiveCell(cellValue, binding.EffectiveType),
      _ => ConvertPrimitiveCell(cellValue, binding.EffectiveType),
    };

  private static object ConvertPrimitiveCell(object cellValue, Type targetType)
  {
    // Already the right type (cell came back typed and the schema
    // matched). No coercion needed.
    if (targetType.IsInstanceOfType(cellValue))
    {
      return cellValue;
    }

    // BCL scalar structs (Guid, TimeSpan, DateTimeOffset, etc.) and
    // user-defined IConvertible-less types: TypeDescriptor consults
    // the type's TypeConverter, which knows how to round-trip from
    // its canonical string form. Tried first because it's a superset
    // of Convert.ChangeType's coverage.
    var converter = TypeConverterCache.For(targetType);
    if (converter is not null && converter.CanConvertFrom(cellValue.GetType()))
    {
      return converter.ConvertFrom(cellValue)!;
    }

    // CLR primitives + string + decimal + DateTime: the IConvertible
    // path. Handles ExcelDataReader's "everything is a double" cells
    // when the schema property is int/long/short/etc.
    return Convert.ChangeType(cellValue, targetType);
  }

  /// <summary>
  /// Per-type cache for <see cref="TypeConverter"/> lookups.
  /// <see cref="TypeDescriptor.GetConverter(Type)"/> is reflection-
  /// driven; caching avoids the per-cell hit.
  /// </summary>
  private static class TypeConverterCache
  {
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, TypeConverter?> _cache = new();

    public static TypeConverter? For(Type type) =>
      _cache.GetOrAdd(type, t => TypeDescriptor.GetConverter(t));
  }

  private static object DecodeEnum(object cellValue, PropertyBinding binding)
  {
    var serialized = cellValue.ToString()
      ?? throw new SchemaMismatchException(
        $"Null cell value for enum field '{binding.FieldName}' "
        + $"(type '{binding.EffectiveType.Name}')."
      );

    if (binding.Enum!.Reverse.TryGetValue(serialized, out var enumValue))
    {
      return enumValue;
    }

    throw new SchemaMismatchException(
      $"'{serialized}' is not a valid serialized value for enum "
      + $"'{binding.EffectiveType.Name}' (field '{binding.FieldName}'). "
      + $"Valid values: {string.Join(", ", binding.Enum.Reverse.Keys.Select(k => $"'{k}'"))}."
    );
  }
}
