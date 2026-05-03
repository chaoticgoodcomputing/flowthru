using System.Reflection;
using System.Text;
using ExcelDataReader;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Serialization;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Serialization;

namespace Flowthru.Core.Data.Storage.Format;

/// <summary>
/// Serializes flat schemas to/from Excel (.xlsx) files using ExcelDataReader.
/// </summary>
/// <typeparam name="TRow">Row type (must be flat and text-serializable)</typeparam>
/// <remarks>
/// <para>
/// <strong>Read-Only:</strong> ExcelDataReader only supports reading Excel files.
/// Calling SerializeRows will throw NotSupportedException.
/// </para>
/// <para>
/// <strong>Sheet Selection:</strong> Reads from specified sheet name.
/// </para>
/// <para>
/// <strong>Null Handling:</strong> Empty cells (DBNull) deserialize to null for nullable
/// properties by default. Catalog authors can additionally treat specific string sentinels
/// as null via the <c>nullValues</c> constructor parameter — for example
/// <c>["", "NA", "N/A", "NULL"]</c> for messy spreadsheet exports. The override applies
/// only to properties declared nullable in the schema (<c>string?</c>, <c>int?</c>, etc.).
/// Non-nullable properties are unaffected.
/// </para>
/// </remarks>
public sealed class ExcelFormatSerializer<TRow> : IFormatSerializer<TRow>
  where TRow : notnull, IFlatSchema, ITextSerializable
{
  /// <summary>The default set of strings treated as null on read for nullable properties.</summary>
  public static readonly IReadOnlyList<string> DefaultNullValues = new[] { string.Empty };

  private readonly string _sheetName;
  private readonly IReadOnlyList<string> _nullValues;

  /// <summary>
  /// Initializes a new instance of the <see cref="ExcelFormatSerializer{TRow}"/> class with
  /// the specified sheet name. Empty cells in nullable properties deserialize to null.
  /// </summary>
  /// <param name="sheetName">The name of the Excel sheet to read from.</param>
  public ExcelFormatSerializer(string sheetName)
    : this(sheetName, DefaultNullValues) { }

  /// <summary>
  /// Initializes a new instance with a custom set of null-representation strings.
  /// </summary>
  /// <param name="sheetName">The name of the Excel sheet to read from.</param>
  /// <param name="nullValues">
  /// Strings that should deserialize to null for nullable properties. Pass
  /// <c>["", "NA", "N/A", "NULL"]</c> for pandas-style handling of messy exports.
  /// </param>
  public ExcelFormatSerializer(string sheetName, IReadOnlyList<string> nullValues)
  {
    _sheetName = sheetName ?? throw new ArgumentNullException(nameof(sheetName));
    _nullValues = nullValues ?? throw new ArgumentNullException(nameof(nullValues));
  }

  /// <summary>
  /// Gets the null-representation strings for this serializer.
  /// </summary>
  public IReadOnlyList<string> NullValues => _nullValues;

  /// <inheritdoc/>
  /// <remarks>
  /// Excel format is read-only (ExcelDataReader does not support writing).
  /// </remarks>
  public StorageTraits Traits => new StorageTraits { CanWrite = false };

  /// <inheritdoc/>
  /// <remarks>
  /// Excel inherits IScalar handling from the planner-driven cell-conversion path
  /// (Phase B3). Flat-only by construction — nested schemas don't compile here due to
  /// the <see cref="Abstractions.IFlatSchema"/> generic constraint. Read-only round-trip
  /// is vacuous, so feature claims are exercised only through the property-mapping
  /// configuration check.
  /// </remarks>
  public FormatRowFeatures RowFeatures => new()
  {
    SupportsIScalar = true,
    SupportsNested = false,
  };

  /// <inheritdoc/>
  public async IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
  {
    // ExcelDataReader requires stream to support seeking
    if (!stream.CanSeek)
    {
      var memoryStream = new MemoryStream();
      await stream.CopyToAsync(memoryStream);
      memoryStream.Position = 0;
      stream = memoryStream;
    }

    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    using var reader = ExcelReaderFactory.CreateReader(stream);

    // Build the planner once per Deserialize call. The plan reports per-property
    // nullability, field-name overrides, and per-kind metadata (Enum / IScalar) the
    // cell-conversion loop below consumes via switch on Kind.
    var plan = PropertyMappingPlanner.Build<TRow>(
      new PropertyMappingPlannerOptions { NullSentinels = _nullValues }
    );

    // Find the target sheet
    do
    {
      if (reader.Name == _sheetName)
      {
        // Read header row
        if (!reader.Read())
        {
          yield break;
        }

        var headers = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
        {
          headers[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
        }

        // Build column index mapping (Excel column header → column index)
        var columnIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
          columnIndexMap[headers[i]] = i;
        }

        // Read data rows
        while (reader.Read())
        {
          // Create new instance using SchemaActivator (supports required members)
          var row = SchemaActivator.CreateInstance<TRow>();

          // Iterate planner bindings rather than reflecting properties directly.
          foreach (var binding in plan.Bindings)
          {
            if (!columnIndexMap.TryGetValue(binding.FieldName, out var columnIndex))
            {
              continue;
            }

            var value = reader.GetValue(columnIndex);

            // Treat configured null sentinels (string-form) and DBNull as null. For
            // nullable bindings, leave the property at its default (null); for non-
            // nullable bindings, fall through to the converter (which surfaces a clear
            // error for required-but-missing data).
            if (binding.IsNullable)
            {
              if (value is null || value == DBNull.Value)
              {
                continue;
              }
              if (value is string strValue && binding.NullSentinels.Contains(strValue))
              {
                continue;
              }
            }
            else if (value is null || value == DBNull.Value)
            {
              continue;
            }

            try
            {
              object convertedValue = ConvertCellValue(value, binding);
              binding.Property.SetValue(row, convertedValue);
            }
            catch
            {
              // Skip properties that can't be converted (matches pre-migration behavior).
            }
          }

          yield return row;
        }

        yield break;
      }
    } while (reader.NextResult());

    throw new InvalidOperationException($"Sheet '{_sheetName}' not found in Excel file.");
  }

  // Per-binding cell conversion. Centralizes the kind-specific decoding the planner
  // tells us applies to each property. Format-specific cell-level encoding (the actual
  // call into Convert.ChangeType / IScalar wrapping / EnumSerializationHelper) stays
  // here in the Excel extension; the planner provides only the metadata.
  private static object ConvertCellValue(object cellValue, PropertyBinding binding)
  {
    return binding.Kind switch
    {
      PropertyKind.Enum => EnumSerializationHelper.ParseEnumFromString(
        binding.EffectiveType,
        cellValue.ToString()!
      ),
      PropertyKind.IScalar => binding.IScalar!.WrappingConstructor.Invoke(
        new[] { Convert.ChangeType(cellValue, binding.IScalar.BackingType) }
      ),
      // Primitive (incl. byte[] and BCL scalar structs): defer to Convert.ChangeType,
      // which handles primitive coercions ExcelDataReader returns (cell typed as double
      // but property typed as int, etc.).
      PropertyKind.Primitive => Convert.ChangeType(cellValue, binding.EffectiveType),
      // Nested: would only reach here if a nested-bearing schema slipped past the
      // IFlatSchema generic constraint — defensive fall-through.
      PropertyKind.Nested => Convert.ChangeType(cellValue, binding.EffectiveType),
      _ => Convert.ChangeType(cellValue, binding.EffectiveType),
    };
  }

  /// <inheritdoc/>
  public Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows)
  {
    throw new NotSupportedException(
      "ExcelFormatSerializer is read-only. Writing Excel files is not supported."
    );
  }

  /// <inheritdoc/>
  public PropertyMappingConfiguration GetPropertyMappingConfiguration()
  {
    return PropertyMappingConfiguration.FromSerializedLabel<TRow>();
  }
}
