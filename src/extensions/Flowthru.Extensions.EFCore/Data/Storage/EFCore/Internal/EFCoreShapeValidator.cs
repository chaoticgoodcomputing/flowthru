using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowthru.Data.Storage.EFCore.Internal;

/// <summary>
/// Pre-flight validator that compares an EF Core entity's expected
/// column shape against the live database table.
/// </summary>
/// <remarks>
/// <para>
/// Connectivity probes (e.g. <c>DbSet.AnyAsync()</c>) confirm a table is
/// reachable but say nothing about whether its columns match what the
/// entity expects. A renamed or dropped column passes connectivity, then
/// fails mid-pipeline when a step actually tries to read or write —
/// wasting all preceding compute.
/// </para>
/// <para>
/// This validator closes that gap for three drift cases: missing columns
/// (declared on the entity but absent in the table), nullability
/// mismatches (entity expects NOT NULL but the column allows NULL — a
/// guaranteed runtime read failure on the first NULL row), and column-
/// type mismatches (e.g. entity property mapped to a Postgres native
/// enum but the column is <c>integer</c>, which only fails at INSERT
/// time as PG <c>42804: column type mismatch</c>).
/// </para>
/// <para>
/// Column-type comparison routes through EF Core's
/// <see cref="IRelationalTypeMappingSource"/>: each side's store-type
/// string is resolved to a canonical <see cref="RelationalTypeMapping"/>
/// and compared by canonical store type + CLR type. This delegates
/// every alias the provider knows about (Npgsql's <c>int</c> ↔
/// <c>integer</c> ↔ <c>int4</c>, SqlServer's <c>varchar(255)</c> ↔
/// <c>varchar</c>) to the provider, so we don't maintain a per-provider
/// alias table.
/// </para>
/// </remarks>
internal static class EFCoreShapeValidator
{
  /// <summary>
  /// Compare the EF Core model's expected columns for
  /// <paramref name="entityClrType"/> against the live table's columns.
  /// Returns a successful <see cref="ValidationResult"/> on agreement
  /// or a SchemaMismatch / InspectionFailure on drift.
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
      // Adapter constructors guard this; defensive skip rather than crash.
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
        IsNullable: p.IsNullable,
        StoreType: p.GetColumnType(storeId)
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

    var typeMappingSource = context.GetService<IRelationalTypeMappingSource>();
    var typeMismatches = new List<TypeMismatch>();
    foreach (var expected in expectedColumns)
    {
      var actual = actualColumns[expected.Name];
      if (string.IsNullOrEmpty(expected.StoreType) || string.IsNullOrEmpty(actual.DataTypeName))
      {
        continue;
      }

      var expectedMapping = typeMappingSource.FindMapping(expected.StoreType);
      var actualMapping = typeMappingSource.FindMapping(actual.DataTypeName);
      if (expectedMapping is null || actualMapping is null)
      {
        // Provider doesn't recognize one or both type strings — best-effort skip.
        continue;
      }

      if (
        !string.Equals(
          expectedMapping.StoreType, actualMapping.StoreType,
          StringComparison.OrdinalIgnoreCase
        ) || expectedMapping.ClrType != actualMapping.ClrType
      )
      {
        typeMismatches.Add(new TypeMismatch(
          ColumnName: expected.Name,
          ExpectedStoreType: expected.StoreType,
          ExpectedCanonical: expectedMapping.StoreType,
          ExpectedClrType: expectedMapping.ClrType,
          ActualStoreType: actual.DataTypeName,
          ActualCanonical: actualMapping.StoreType,
          ActualClrType: actualMapping.ClrType
        ));
      }
    }

    if (typeMismatches.Count > 0)
    {
      var summaries = typeMismatches.Select(m =>
        $"{m.ColumnName} (entity: {m.ExpectedStoreType} → {m.ExpectedClrType.Name}; "
          + $"database: {m.ActualStoreType} → {m.ActualClrType.Name})"
      );
      return ValidationResult.Failure(
        catalogKey: catalogKey,
        errorType: ValidationErrorType.SchemaMismatch,
        message: $"Table '{FormatTableName(schema, tableName)}' column-type mismatch with entity '{entityClrType.Name}': "
          + string.Join("; ", summaries),
        details: $"Via {context.GetType().Name} on {GetConnectionDescription(context)}. "
          + "EFCore's IProperty.GetColumnType() reports the provider-specific store type for the entity "
          + "property; the schema reader's DataTypeName reports what the database actually has."
      );
    }

    return ValidationResult.Success();
  }

  /// <summary>
  /// Read the live column shape for <paramref name="tableName"/> via a
  /// zero-row probe (<c>SELECT * FROM ... WHERE 1 = 0</c>) and inspect
  /// the resulting reader's schema. Portable across every ADO.NET
  /// provider — relies only on universal SQL and
  /// <see cref="IDataReader.GetSchemaTable"/>, avoiding provider-specific
  /// gaps in <c>DbConnection.GetSchema</c> support.
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
        await connection.OpenAsync(ct).ConfigureAwait(false);
      }

      await using var cmd = connection.CreateCommand();
      cmd.CommandText = $"SELECT * FROM {qualifiedTable} WHERE 1 = 0";
      cmd.CommandType = CommandType.Text;

      // KeyInfo is required for Npgsql to populate AllowDBNull from
      // pg_attribute.attnotnull. SQLite and SqlClient tolerate it
      // without behaviour change.
      await using var reader = await cmd.ExecuteReaderAsync(
        CommandBehavior.SchemaOnly | CommandBehavior.SingleResult | CommandBehavior.KeyInfo,
        ct
      ).ConfigureAwait(false);

      return BuildColumnDictionary(reader);
    }
    finally
    {
      if (openedHere && connection.State == ConnectionState.Open)
      {
        await connection.CloseAsync().ConfigureAwait(false);
      }
    }
  }

  private static Dictionary<string, ActualColumn> BuildColumnDictionary(DbDataReader reader)
  {
    var dict = new Dictionary<string, ActualColumn>(StringComparer.OrdinalIgnoreCase);

    var schemaTable = reader.GetSchemaTable();
    if (schemaTable is null)
    {
      // Provider couldn't materialise a schema. Fall back to FieldCount /
      // GetName: column presence only, nullability + type checks become no-ops.
      for (var i = 0; i < reader.FieldCount; i++)
      {
        var name = reader.GetName(i);
        if (!string.IsNullOrEmpty(name))
        {
          dict[name] = new ActualColumn(name, IsNullable: true, DataTypeName: null);
        }
      }
      return dict;
    }

    foreach (DataRow row in schemaTable.Rows)
    {
      var nameObj = row["ColumnName"];
      if (nameObj is null || nameObj == DBNull.Value) continue;
      var name = nameObj.ToString()!;
      var isNullable = ParseAllowDbNull(
        schemaTable.Columns.Contains("AllowDBNull") ? row["AllowDBNull"] : null
      );
      var dataTypeName = schemaTable.Columns.Contains("DataTypeName")
        ? row["DataTypeName"]?.ToString()
        : null;
      dict[name] = new ActualColumn(name, isNullable, dataTypeName);
    }
    return dict;
  }

  private static bool ParseAllowDbNull(object? value)
  {
    // When AllowDBNull is unreadable, treat the column as NOT NULL so
    // the nullability check is a safe no-op. The opposite default
    // would reliably false-flag every non-nullable entity property.
    if (value is null || value == DBNull.Value) return false;
    if (value is bool b) return b;
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

  private record ExpectedColumn(string Name, bool IsNullable, string? StoreType);
  private record ActualColumn(string Name, bool IsNullable, string? DataTypeName);
  private record TypeMismatch(
    string ColumnName,
    string ExpectedStoreType,
    string ExpectedCanonical,
    Type ExpectedClrType,
    string ActualStoreType,
    string ActualCanonical,
    Type ActualClrType
  );
}
