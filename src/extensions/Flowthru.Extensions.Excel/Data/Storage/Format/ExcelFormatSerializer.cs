using System.Text;
using ExcelDataReader;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;

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
/// </remarks>
public sealed class ExcelFormatSerializer<TRow> : IFormatSerializer<TRow>
  where TRow : notnull, IFlatSchema, ITextSerializable
{
    private readonly string _sheetName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelFormatSerializer{TRow}"/> class with the specified sheet name.
    /// </summary>
    /// <param name="sheetName">The name of the Excel sheet to read from.</param>
    public ExcelFormatSerializer(string sheetName)
    {
        _sheetName = sheetName;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Excel format is read-only (ExcelDataReader does not support writing).
    /// </remarks>
    public StorageTraits Traits => new StorageTraits { CanWrite = false };

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

                // Build property map using SerializedLabel attributes
                var propertyMap = PropertyMappingHelper.BuildPropertyMap<TRow>();

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

                    // Set properties from Excel columns using the property map
                    foreach (var (fieldName, property) in propertyMap)
                    {
                        // Try to find column by field name (from SerializedLabel or property name)
                        if (columnIndexMap.TryGetValue(fieldName, out var columnIndex))
                        {
                            var value = reader.GetValue(columnIndex);
                            if (value != null && value != DBNull.Value)
                            {
                                try
                                {
                                    // Handle nullable properties
                                    var targetType =
                                      Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                                    object convertedValue;

                                    // Special handling for enum types with [SerializedEnum] attributes
                                    if (targetType.IsEnum)
                                    {
                                        string stringValue = value.ToString()!;
                                        convertedValue = Serialization.EnumSerializationHelper.ParseEnumFromString(
                                          targetType,
                                          stringValue
                                        );
                                    }
                                    else
                                    {
                                        convertedValue = Convert.ChangeType(value, targetType);
                                    }

                                    // SetValue works on init properties via reflection
                                    property.SetValue(row, convertedValue);
                                }
                                catch
                                {
                                    // Skip properties that can't be converted
                                }
                            }
                        }
                    }

                    yield return row;
                }

                yield break;
            }
        } while (reader.NextResult());

        throw new InvalidOperationException($"Sheet '{_sheetName}' not found in Excel file.");
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
