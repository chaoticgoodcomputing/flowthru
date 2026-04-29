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
/// This validator closes that gap for three drift cases: missing columns
/// (column declared on the entity but absent in the table), nullability mismatches
/// (entity expects NOT NULL but the column allows NULL, which guarantees a runtime
/// read failure as soon as a NULL row is materialized), and column-type mismatches
/// (e.g. entity property mapped to a PostgreSQL native enum but the column is
/// <c>integer</c>, which only fails at <c>INSERT … SELECT</c> time as
/// <c>42804: column type mismatch</c>).
/// </para>
/// <para>
/// Column-type comparison routes through EF Core's own
/// <see cref="IRelationalTypeMappingSource"/>: each column's <c>DataTypeName</c>
/// (from the schema reader) and the entity property's expected store type are
/// each resolved through <c>FindMapping</c>, then compared by canonical
/// <see cref="RelationalTypeMapping.StoreType"/> and the mapping's CLR
/// type. The provider's own resolver
/// handles every alias the provider knows about (e.g. Npgsql's <c>int</c> ↔
/// <c>integer</c> ↔ <c>int4</c>), so we don't maintain a per-provider alias
/// table. Mirrors EFCore's internal pattern in <c>MigrationsModelDiffer</c>
/// (model-vs-model diff) and <c>ScaffoldingTypeMapper</c> (DB-to-CLR resolution).
/// </para>
/// </remarks>
internal static class EFCoreShapeValidator
{
  /// <summary>
  /// Compares the EF Core model's expected columns for <paramref name="entityClrType"/>
  /// against the live table's columns, surfacing missing columns, nullability drift,
  /// and column-type drift as <see cref="ValidationErrorType.SchemaMismatch"/>.
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

    // Column-type compatibility check. Resolves each side through the provider's
    // IRelationalTypeMappingSource so aliases (int ↔ integer ↔ int4 on Npgsql,
    // varchar(255) ↔ varchar on SqlServer, etc.) are handled by the provider rather
    // than by us. A genuine mismatch — e.g. table column INTEGER but entity property
    // mapped to a Postgres-native enum (`client.poll_element_type`) — surfaces here
    // before the pipeline runs and discovers PG error 42804 at INSERT time.
    var typeMappingSource = context.GetService<IRelationalTypeMappingSource>();
    var typeMismatches = new List<TypeMismatch>();
    foreach (var expected in expectedColumns)
    {
      var actual = actualColumns[expected.Name];
      if (string.IsNullOrEmpty(expected.StoreType) || string.IsNullOrEmpty(actual.DataTypeName))
      {
        // Either side missing a type string we can resolve. Skip rather than guess.
        continue;
      }

      var expectedMapping = typeMappingSource.FindMapping(expected.StoreType);
      var actualMapping = typeMappingSource.FindMapping(actual.DataTypeName);

      if (expectedMapping is null || actualMapping is null)
      {
        // Provider doesn't recognize one or both type strings. Treat as
        // best-effort skip — we'd rather miss an obscure type than false-flag.
        continue;
      }

      if (
        !string.Equals(
          expectedMapping.StoreType,
          actualMapping.StoreType,
          StringComparison.OrdinalIgnoreCase
        ) || expectedMapping.ClrType != actualMapping.ClrType
      )
      {
        typeMismatches.Add(
          new TypeMismatch(
            ColumnName: expected.Name,
            ExpectedStoreType: expected.StoreType,
            ExpectedCanonical: expectedMapping.StoreType,
            ExpectedClrType: expectedMapping.ClrType,
            ActualStoreType: actual.DataTypeName,
            ActualCanonical: actualMapping.StoreType,
            ActualClrType: actualMapping.ClrType
          )
        );
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
          + "property; the schema reader's DataTypeName reports what the database actually has. "
          + "A mismatch indicates the schema diverged from the entity model — e.g. a missing "
          + "Npgsql.MapEnum() registration, a column re-typed in DbUp without re-running, or a "
          + "DbContext registered twice with different conventions."
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

      // KeyInfo is required for Npgsql to populate AllowDBNull from
      // pg_attribute.attnotnull — without it, GetSchemaTable returns DBNull
      // for the AllowDBNull column on every row, which would make every
      // NOT NULL entity property look like a nullability mismatch (verified
      // empirically against Npgsql 10.0.1). The flag is a hint, not a
      // contract; SQLite and SqlClient tolerate it without behavior change.
      await using var reader = await cmd.ExecuteReaderAsync(
        CommandBehavior.SchemaOnly | CommandBehavior.SingleResult | CommandBehavior.KeyInfo,
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
    // to FieldCount/GetName (column presence only — nullability + type checks
    // gracefully degrade to no-op rather than false-flagging).
    var schemaTable = reader.GetSchemaTable();
    if (schemaTable is null)
    {
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
      if (nameObj is null || nameObj == DBNull.Value)
      {
        continue;
      }
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
    // Provider differences: Microsoft.Data.Sqlite types this column as bool;
    // Npgsql and SqlClient return "YES"/"NO"; some return "1"/"0".
    if (value is null || value == DBNull.Value)
    {
      // When AllowDBNull is unreadable, treat the column as NOT NULL so the
      // nullability check becomes a safe no-op for it. The opposite default
      // (assume nullable) reliably false-flags every non-nullable entity
      // property — a regression we hit on Npgsql before adding KeyInfo above.
      // Defaulting to false defers detection of real drift to runtime for
      // this column rather than blocking pipelines that would succeed.
      return false;
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
