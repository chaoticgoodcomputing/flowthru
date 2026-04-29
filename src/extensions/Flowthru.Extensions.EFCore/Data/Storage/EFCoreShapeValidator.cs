using System.Data;
using System.Data.Common;
using Flowthru.Core.Data.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Pre-flight validator that compares an EF Core entity's expected column shape
/// against the live database table.
/// </summary>
/// <remarks>
/// <para>
/// Connectivity probes (e.g. <c>DbSet.AnyAsync()</c>) confirm a table is reachable
/// but say nothing about whether its columns match what the entity expects. A
/// renamed or dropped column passes connectivity, then fails mid-pipeline when a
/// step actually tries to read or write — wasting all preceding compute.
/// </para>
/// <para>
/// This validator closes that gap for the most common drift cases: missing columns
/// (column declared on the entity but absent in the table) and nullability
/// mismatches (entity expects NOT NULL but the column allows NULL, which guarantees
/// a runtime read failure as soon as a NULL row is materialized).
/// </para>
/// <para>
/// Type comparison is intentionally out of scope: provider-specific type-name
/// differences (<c>INTEGER</c> vs <c>int4</c> vs <c>int</c>) make a portable
/// comparison fragile, and EF's own materialization will surface most type-shape
/// problems via the existing sample-read path in <c>InspectShallow</c>.
/// </para>
/// </remarks>
internal static class EFCoreShapeValidator
{
  /// <summary>
  /// Compares the EF Core model's expected columns for <paramref name="entityClrType"/>
  /// against the live table's columns, surfacing missing columns and nullability
  /// drift as <see cref="ValidationErrorType.SchemaMismatch"/>.
  /// </summary>
  public static async Task<ValidationResult> ValidateAsync(
    DbContext context,
    Type entityClrType,
    string catalogKey,
    CancellationToken ct
  )
  {
    var efEntityType = context.Model.FindEntityType(entityClrType);
    if (efEntityType is null)
    {
      // Adapter constructors guard this, but be defensive: skip rather than crash.
      return ValidationResult.Success();
    }

    var tableName = efEntityType.GetTableName();
    if (string.IsNullOrEmpty(tableName))
    {
      // Keyless / SQL-query-backed entities have no single table to inspect.
      return ValidationResult.Success();
    }
    var schema = efEntityType.GetSchema();
    var storeId = StoreObjectIdentifier.Table(tableName, schema);

    var expectedColumns = efEntityType
      .GetProperties()
      .Select(p => new ExpectedColumn(
        Name: p.GetColumnName(storeId) ?? p.Name,
        IsNullable: p.IsNullable
      ))
      .ToList();

    Dictionary<string, ActualColumn> actualColumns;
    try
    {
      actualColumns = await ReadActualColumnsAsync(context, tableName, schema, ct);
    }
    catch (Exception ex)
    {
      return ValidationResult.Failure(
        catalogKey: catalogKey,
        errorType: ValidationErrorType.InspectionFailure,
        message: $"Failed to read column metadata for table '{FormatTableName(schema, tableName)}'",
        details: $"Via {context.GetType().Name} on {GetConnectionDescription(context)}: {ex.Message}"
      );
    }

    if (actualColumns.Count == 0)
    {
      // Reachable table that exposes no columns is a metadata anomaly, not a
      // schema problem we can describe. Surface it transparently.
      return ValidationResult.Failure(
        catalogKey: catalogKey,
        errorType: ValidationErrorType.InspectionFailure,
        message: $"Could not retrieve column metadata for table '{FormatTableName(schema, tableName)}'",
        details: $"Via {context.GetType().Name} on {GetConnectionDescription(context)}: "
          + "the table is reachable but the provider returned no columns."
      );
    }

    var missing = expectedColumns
      .Where(e => !actualColumns.ContainsKey(e.Name))
      .Select(e => e.Name)
      .ToList();

    if (missing.Count > 0)
    {
      return ValidationResult.Failure(
        catalogKey: catalogKey,
        errorType: ValidationErrorType.SchemaMismatch,
        message: $"Table '{FormatTableName(schema, tableName)}' is missing column(s) expected by entity '{entityClrType.Name}': "
          + string.Join(", ", missing),
        details: $"Via {context.GetType().Name} on {GetConnectionDescription(context)}. "
          + $"Expected columns: [{string.Join(", ", expectedColumns.Select(c => c.Name))}]. "
          + $"Actual columns: [{string.Join(", ", actualColumns.Keys)}]."
      );
    }

    var nullabilityMismatches = expectedColumns
      .Where(e => !e.IsNullable && actualColumns[e.Name].IsNullable)
      .Select(e => e.Name)
      .ToList();

    if (nullabilityMismatches.Count > 0)
    {
      return ValidationResult.Failure(
        catalogKey: catalogKey,
        errorType: ValidationErrorType.SchemaMismatch,
        message: $"Table '{FormatTableName(schema, tableName)}' nullability mismatch with entity '{entityClrType.Name}': "
          + $"column(s) {string.Join(", ", nullabilityMismatches)} are NOT NULL on the entity but allow NULL in the database",
        details: $"Via {context.GetType().Name} on {GetConnectionDescription(context)}. "
          + "NULL values in these columns will cause runtime read failures when materializing the entity."
      );
    }

    return ValidationResult.Success();
  }

  /// <summary>
  /// Reads the live column shape for <paramref name="tableName"/> by issuing a
  /// zero-row probe (<c>SELECT * FROM ... WHERE 1 = 0</c>) and inspecting the
  /// resulting reader's schema. This is portable across every ADO.NET provider
  /// because it relies only on universal SQL and <see cref="IDataReader.GetSchemaTable"/>,
  /// avoiding provider-specific gaps in <c>DbConnection.GetSchema</c> support
  /// (notably Microsoft.Data.Sqlite, which does not implement the Columns
  /// collection).
  /// </summary>
  private static async Task<Dictionary<string, ActualColumn>> ReadActualColumnsAsync(
    DbContext context,
    string tableName,
    string? schema,
    CancellationToken ct
  )
  {
    var sqlGen = context.GetService<ISqlGenerationHelper>();
    var qualifiedTable = sqlGen.DelimitIdentifier(tableName, schema);

    var connection = context.Database.GetDbConnection();
    var openedHere = connection.State != ConnectionState.Open;
    try
    {
      if (openedHere)
      {
        await connection.OpenAsync(ct);
      }

      await using var cmd = connection.CreateCommand();
      cmd.CommandText = $"SELECT * FROM {qualifiedTable} WHERE 1 = 0";
      cmd.CommandType = CommandType.Text;

      await using var reader = await cmd.ExecuteReaderAsync(
        CommandBehavior.SchemaOnly | CommandBehavior.SingleResult,
        ct
      );

      return BuildColumnDictionary(reader);
    }
    finally
    {
      if (openedHere && connection.State == ConnectionState.Open)
      {
        await connection.CloseAsync();
      }
    }
  }

  private static Dictionary<string, ActualColumn> BuildColumnDictionary(DbDataReader reader)
  {
    var dict = new Dictionary<string, ActualColumn>(StringComparer.OrdinalIgnoreCase);

    // GetSchemaTable is the standard ADO.NET shape carrier — it's required to
    // include ColumnName and AllowDBNull. A few providers may return null if
    // they couldn't materialize a schema for the query; in that case fall back
    // to FieldCount/GetName (column presence only — nullability check
    // gracefully degrades to no-op rather than false-flagging).
    var schemaTable = reader.GetSchemaTable();
    if (schemaTable is null)
    {
      for (var i = 0; i < reader.FieldCount; i++)
      {
        var name = reader.GetName(i);
        if (!string.IsNullOrEmpty(name))
        {
          dict[name] = new ActualColumn(name, IsNullable: true);
        }
      }
      return dict;
    }

    foreach (DataRow row in schemaTable.Rows)
    {
      var nameObj = row["ColumnName"];
      if (nameObj is null || nameObj == DBNull.Value)
      {
        continue;
      }
      var name = nameObj.ToString()!;
      var isNullable = ParseAllowDbNull(
        schemaTable.Columns.Contains("AllowDBNull") ? row["AllowDBNull"] : null
      );
      dict[name] = new ActualColumn(name, isNullable);
    }
    return dict;
  }

  private static bool ParseAllowDbNull(object? value)
  {
    if (value is null || value == DBNull.Value)
    {
      return true; // Unknown → lenient (don't false-flag).
    }
    if (value is bool b)
    {
      return b;
    }
    var str = value.ToString() ?? string.Empty;
    return str.Equals("YES", StringComparison.OrdinalIgnoreCase)
      || str.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
      || str == "1";
  }

  private static string FormatTableName(string? schema, string table) =>
    string.IsNullOrEmpty(schema) ? table : $"{schema}.{table}";

  private static string GetConnectionDescription(DbContext context)
  {
    try
    {
      var conn = context.Database.GetDbConnection();
      var dataSource = conn.DataSource;
      var database = conn.Database;
      return string.IsNullOrEmpty(dataSource) ? database : $"{dataSource}/{database}";
    }
    catch
    {
      return "(connection info unavailable)";
    }
  }

  private record ExpectedColumn(string Name, bool IsNullable);

  private record ActualColumn(string Name, bool IsNullable);
}
