using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Flowthru.Data.Storage.EFCore.Npgsql.Internal;

/// <summary>
/// The physical COPY target resolved from the EF model at adapter
/// construction: the schema-qualified, identifier-quoted table plus the
/// deterministic column list, pre-rendered into the three SQL statements
/// the transfer rungs execute. Resolution is model-metadata only — no
/// connection is opened — so it is safe (and zero-I/O) at construction
/// time.
/// </summary>
/// <param name="QualifiedTable">Quoted (and schema-qualified when the entity maps to an explicit schema) table identifier.</param>
/// <param name="Columns">Physical column names, in EF model property order — the shared order both endpoints derive from the same entity type.</param>
/// <param name="ExportSql">The <c>COPY ... TO STDOUT (FORMAT BINARY)</c> statement.</param>
/// <param name="ImportSql">The <c>COPY ... FROM STDIN (FORMAT BINARY)</c> statement.</param>
/// <param name="TruncateSql">The <c>TRUNCATE</c> statement the Replace import mode runs inside the transfer transaction.</param>
internal sealed record NpgsqlCopyTarget(
  string QualifiedTable,
  IReadOnlyList<string> Columns,
  string ExportSql,
  string ImportSql,
  string TruncateSql
)
{
  /// <summary>
  /// Resolve the COPY target for <paramref name="entityClrType"/> from
  /// <paramref name="context"/>'s model. The column list is derived from
  /// the entity's mapped properties — never guessed from CLR member
  /// names — and excludes store-computed columns (PostgreSQL rejects
  /// supplying values for them). Explicit column lists on both COPY
  /// statements make the pairing independent of each database's physical
  /// column order.
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// The entity is not configured in the context, is not mapped to a
  /// table (view- or query-mapped entities cannot COPY), or maps no
  /// copyable columns.
  /// </exception>
  internal static NpgsqlCopyTarget Resolve(DbContext context, Type entityClrType)
  {
    var entityType = context.Model.FindEntityType(entityClrType)
      ?? throw new InvalidOperationException(
        $"Entity type '{entityClrType.Name}' is not configured in DbContext "
        + $"'{context.GetType().Name}'.");

    var tableName = entityType.GetTableName()
      ?? throw new InvalidOperationException(
        $"Entity type '{entityClrType.Name}' in DbContext '{context.GetType().Name}' is not "
        + "mapped to a table, so it cannot participate in a raw binary COPY transfer. "
        + "View- and query-mapped entities have no physical table to COPY.");

    var schema = entityType.GetSchema();
    var storeObject = StoreObjectIdentifier.Table(tableName, schema);

    var columns = new List<string>();
    foreach (var property in entityType.GetProperties())
    {
      var columnName = property.GetColumnName(storeObject);
      if (columnName is null) continue; // property not mapped to this table
      if (property.GetComputedColumnSql(storeObject) is not null) continue; // store-computed
      columns.Add(columnName);
    }

    if (columns.Count == 0)
    {
      throw new InvalidOperationException(
        $"Entity type '{entityClrType.Name}' maps no copyable columns on table '{tableName}'.");
    }

    var qualifiedTable = schema is null
      ? Quote(tableName)
      : $"{Quote(schema)}.{Quote(tableName)}";
    var columnList = string.Join(", ", columns.Select(Quote));

    return new NpgsqlCopyTarget(
      QualifiedTable: qualifiedTable,
      Columns: columns,
      ExportSql: $"COPY {qualifiedTable} ({columnList}) TO STDOUT (FORMAT BINARY)",
      ImportSql: $"COPY {qualifiedTable} ({columnList}) FROM STDIN (FORMAT BINARY)",
      TruncateSql: $"TRUNCATE TABLE {qualifiedTable}"
    );
  }

  /// <summary>PostgreSQL identifier quoting: wrap in double quotes, double any embedded quote.</summary>
  private static string Quote(string identifier) =>
    "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
