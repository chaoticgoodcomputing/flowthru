using System.Text;
using ExcelDataReader;
using Flowthru.Abstractions;
using Flowthru.Data.Storage;

namespace Flowthru.Data.Storage.Format;

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
  where TRow : IFlatSchema, ITextSerializable, new()
{
  private readonly string _sheetName;

  public ExcelFormatSerializer(string sheetName)
  {
    _sheetName = sheetName;
  }

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
          yield break;

        var headers = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
        {
          headers[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
        }

        // Get all properties once outside the loop
        var properties = typeof(TRow).GetProperties().ToList();

        // Build column name mapping (supports both exact match and snake_case → PascalCase)
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
          var header = headers[i];
          columnMap[header] = i;

          // Also map snake_case to PascalCase (e.g., "company_id" → "CompanyId")
          var pascalCase = ConvertSnakeCaseToPascalCase(header);
          if (!columnMap.ContainsKey(pascalCase))
          {
            columnMap[pascalCase] = i;
          }
        }

        // Read data rows
        while (reader.Read())
        {
          // Create new instance (works with both classes and records)
          var row = new TRow();

          // Set properties from Excel columns
          foreach (var property in properties)
          {
            // Try to find column by property name (case-insensitive, with snake_case support)
            if (columnMap.TryGetValue(property.Name, out var columnIndex))
            {
              var value = reader.GetValue(columnIndex);
              if (value != null && value != DBNull.Value)
              {
                try
                {
                  // Handle nullable properties
                  var targetType =
                    Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                  var convertedValue = Convert.ChangeType(value, targetType);
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

  /// <summary>
  /// Converts snake_case column names to PascalCase property names.
  /// Example: "company_id" → "CompanyId", "review_scores_rating" → "ReviewScoresRating"
  /// </summary>
  private static string ConvertSnakeCaseToPascalCase(string snakeCase)
  {
    if (string.IsNullOrWhiteSpace(snakeCase))
    {
      return snakeCase;
    }

    var parts = snakeCase.Split('_', StringSplitOptions.RemoveEmptyEntries);
    return string.Concat(
      parts.Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant())
    );
  }
}
