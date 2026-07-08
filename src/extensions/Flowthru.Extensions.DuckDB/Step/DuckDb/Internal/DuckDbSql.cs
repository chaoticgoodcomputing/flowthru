namespace Flowthru.Step.DuckDb.Internal;

/// <summary>
/// SQL text assembly shared by the runtime engine
/// (<see cref="InProcessDuckDbEngine"/>) and the hermetic pre-flight
/// check (<see cref="DuckDbSqlSchemaCheck"/>) — one quoting/trimming
/// discipline so the SQL that pre-flight binds is byte-identical to the
/// SQL the engine executes.
/// </summary>
internal static class DuckDbSql
{
  /// <summary>Strip trailing whitespace and statement terminators so the
  /// query embeds cleanly in <c>DESCRIBE ...</c> and <c>COPY (...)</c>.</summary>
  public static string TrimTerminator(string sql) => sql.TrimEnd().TrimEnd(';').TrimEnd();

  /// <summary>Quote an identifier (relation/column name), doubling embedded quotes.</summary>
  public static string QuoteIdentifier(string identifier) =>
    $"\"{identifier.Replace("\"", "\"\"")}\"";

  /// <summary>Quote a string literal (file path, setting), doubling embedded quotes.</summary>
  public static string QuoteLiteral(string value) =>
    $"'{value.Replace("'", "''")}'";
}
